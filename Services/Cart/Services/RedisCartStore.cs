using System.Text.Json;
using Common.Models;
using StackExchange.Redis;

namespace Cart.Services;

/// <summary>
/// Keeps each cart in two Valkey hashes so every change is a native atomic command, not a read-modify-write
/// </summary>
/// <remarks>
/// <c>cart:{session}:qty</c> maps productId → quantity and is changed with HINCRBY/HSET.
/// <c>cart:{session}:items</c> maps productId → JSON name and price.
/// The braces are a hash tag, so both keys live on the same slot in a cluster and can share a transaction.
/// Each write runs as one MULTI/EXEC that also reads the cart back, so the caller gets a consistent snapshot
/// in a single round trip. Every write refreshes the TTL on both keys.
/// </remarks>
public class RedisCartStore : ICartStore
{
    private readonly IRedisService _redis;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCartStore"/> class.
    /// </summary>
    public RedisCartStore(IRedisService redis)
    {
        _redis = redis;
    }

    /// <summary>
    /// Gets the key holding a cart's quantities
    /// </summary>
    public static RedisKey QuantityKey(string sessionId) => $"cart:{{{sessionId}}}:qty";

    /// <summary>
    /// Gets the key holding a cart's product names and prices
    /// </summary>
    public static RedisKey ItemsKey(string sessionId) => $"cart:{{{sessionId}}}:items";

    /// <inheritdoc />
    public async Task<List<CartItem>> GetItemsAsync(string sessionId)
    {
        var transaction = _redis.Database.CreateTransaction();
        var snapshot = ReadBack(transaction, sessionId);
        await transaction.ExecuteAsync();
        return await snapshot;
    }

    /// <inheritdoc />
    public async Task<List<CartItem>> AddItemAsync(string sessionId, CartItem item)
    {
        var transaction = _redis.Database.CreateTransaction();
        _ = transaction.HashIncrementAsync(QuantityKey(sessionId), item.ProductId, item.Quantity);
        _ = transaction.HashSetAsync(ItemsKey(sessionId), item.ProductId, Describe(item));
        Touch(transaction, sessionId);
        var snapshot = ReadBack(transaction, sessionId);

        await transaction.ExecuteAsync();
        return await snapshot;
    }

    /// <inheritdoc />
    public async Task<List<CartItem>?> SetQuantityAsync(string sessionId, string productId, int quantity)
    {
        if (quantity <= 0)
        {
            return await RemoveItemAsync(sessionId, productId);
        }

        var transaction = _redis.Database.CreateTransaction();
        transaction.AddCondition(Condition.HashExists(QuantityKey(sessionId), productId));
        _ = transaction.HashSetAsync(QuantityKey(sessionId), productId, quantity);
        Touch(transaction, sessionId);
        var snapshot = ReadBack(transaction, sessionId);

        return await transaction.ExecuteAsync() ? await snapshot : null;
    }

    /// <inheritdoc />
    public async Task<List<CartItem>?> RemoveItemAsync(string sessionId, string productId)
    {
        var transaction = _redis.Database.CreateTransaction();
        transaction.AddCondition(Condition.HashExists(QuantityKey(sessionId), productId));
        _ = transaction.HashDeleteAsync(QuantityKey(sessionId), productId);
        _ = transaction.HashDeleteAsync(ItemsKey(sessionId), productId);
        Touch(transaction, sessionId);
        var snapshot = ReadBack(transaction, sessionId);

        return await transaction.ExecuteAsync() ? await snapshot : null;
    }

    /// <inheritdoc />
    public async Task ClearAsync(string sessionId)
    {
        await _redis.Database.KeyDeleteAsync(new[] { QuantityKey(sessionId), ItemsKey(sessionId) });
    }

    private void Touch(ITransaction transaction, string sessionId)
    {
        _ = transaction.KeyExpireAsync(QuantityKey(sessionId), _redis.CartTtl);
        _ = transaction.KeyExpireAsync(ItemsKey(sessionId), _redis.CartTtl);
    }

    private static async Task<List<CartItem>> ReadBack(ITransaction transaction, string sessionId)
    {
        var quantities = transaction.HashGetAllAsync(QuantityKey(sessionId));
        var details = transaction.HashGetAllAsync(ItemsKey(sessionId));

        var detailsById = (await details).ToDictionary(e => e.Name.ToString(), e => e.Value);
        var items = new List<CartItem>();

        foreach (var entry in await quantities)
        {
            var productId = entry.Name.ToString();
            var detail = detailsById.TryGetValue(productId, out var json)
                ? JsonSerializer.Deserialize<ItemDetail>(json.ToString())
                : null;

            items.Add(new CartItem
            {
                ProductId = productId,
                Name = detail?.Name,
                Price = detail?.Price ?? 0,
                Quantity = (int)entry.Value
            });
        }

        return items;
    }

    private static string Describe(CartItem item) =>
        JsonSerializer.Serialize(new ItemDetail(item.Name, item.Price));

    private sealed record ItemDetail(string? Name, decimal Price);
}
