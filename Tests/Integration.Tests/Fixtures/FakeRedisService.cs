using Cart.Services;
using Common.Models;

namespace Integration.Tests.Fixtures;

/// <summary>
/// A fake implementation of IRedisService for testing
/// </summary>
public class FakeRedisService : IRedisService
{
    private readonly Dictionary<string, object> _cache = new();
    private bool _isConnected = true;

    /// <summary>
    /// Gets a value indicating whether the service is connected
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Gets the cart TTL
    /// </summary>
    public TimeSpan CartTtl => TimeSpan.FromMinutes(15);

    /// <summary>
    /// Connects to Redis
    /// </summary>
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Connects to Redis with retry
    /// </summary>
    public Task ConnectWithRetryAsync(int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a value from Redis
    /// </summary>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var value) && value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }

        if (key.StartsWith("cart:"))
        {
            // Return a default cart for testing
            if (typeof(T) == typeof(List<CartItem>))
            {
                var cartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductId = "test-product-id",
                        Name = "Test Product",
                        Price = 19.99m,
                        Quantity = 1
                    }
                };

                return Task.FromResult((T?)(object)cartItems);
            }
        }

        return Task.FromResult<T?>(default);
    }

    /// <summary>
    /// Sets a value in Redis
    /// </summary>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        _cache[key] = value!;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a value from Redis
    /// </summary>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the service
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _cache.Clear();
        return ValueTask.CompletedTask;
    }
}
