using Common.Models;

namespace Cart.Services;

/// <summary>
/// Service for cart operations
/// </summary>
public class CartService
{
    private readonly ILogger<CartService> _logger;
    private readonly ICartStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartService"/> class.
    /// </summary>
    public CartService(ILogger<CartService> logger, ICartStore store)
    {
        _logger = logger;
        _store = store;
    }

    /// <summary>
    /// Gets a cart by session ID
    /// </summary>
    public Task<CartResponse> GetCartAsync(string sessionId, CancellationToken cancellationToken = default) =>
        RunAsync(sessionId, "getting cart", async () =>
            Success(sessionId, "Cart retrieved successfully", await _store.GetItemsAsync(sessionId)));

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    public Task<CartResponse> AddItemAsync(string sessionId, CartItem item,
        CancellationToken cancellationToken = default) =>
        RunAsync(sessionId, "adding item to cart", async () =>
            Success(sessionId, "Item added to cart successfully", await _store.AddItemAsync(sessionId, item)));

    /// <summary>
    /// Updates an item in the cart; a quantity of 0 or less removes it
    /// </summary>
    public Task<CartResponse> UpdateItemAsync(string sessionId, string productId, int quantity,
        CancellationToken cancellationToken = default) =>
        RunAsync(sessionId, "updating item in cart", async () =>
        {
            var items = await _store.SetQuantityAsync(sessionId, productId, quantity);
            return items == null
                ? await NotInCartAsync(sessionId, productId)
                : Success(sessionId, "Cart updated successfully", items);
        });

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    public Task<CartResponse> RemoveItemAsync(string sessionId, string productId,
        CancellationToken cancellationToken = default) =>
        RunAsync(sessionId, "removing item from cart", async () =>
        {
            var items = await _store.RemoveItemAsync(sessionId, productId);
            return items == null
                ? await NotInCartAsync(sessionId, productId)
                : Success(sessionId, "Item removed from cart successfully", items);
        });

    /// <summary>
    /// Clears the cart
    /// </summary>
    public Task<CartResponse> ClearCartAsync(string sessionId, CancellationToken cancellationToken = default) =>
        RunAsync(sessionId, "clearing cart", async () =>
        {
            await _store.ClearAsync(sessionId);
            return Success(sessionId, "Cart cleared successfully", new List<CartItem>());
        });

    private async Task<CartResponse> RunAsync(string sessionId, string action, Func<Task<CartResponse>> operation)
    {
        try
        {
            var response = await operation();
            _logger.LogInformation("Finished {Action} for session ID: {SessionId}, Items: {ItemCount}, Total: {TotalPrice}",
                action, sessionId, response.ItemCount, response.TotalPrice);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error {Action} for session ID: {SessionId}", action, sessionId);
            return new CartResponse
            {
                Success = false,
                Error = $"Error {action}: {ex.Message}",
                SessionId = sessionId,
                Items = new List<CartItem>()
            };
        }
    }

    private async Task<CartResponse> NotInCartAsync(string sessionId, string productId)
    {
        _logger.LogWarning("Item not found in cart: {ProductId}", productId);
        var response = Success(sessionId, null, await _store.GetItemsAsync(sessionId));
        response.Success = false;
        response.Error = $"Item with ID {productId} not found in cart";
        return response;
    }

    private static CartResponse Success(string sessionId, string? message, List<CartItem> items) => new()
    {
        Success = true,
        Message = message,
        SessionId = sessionId,
        Items = items,
        TotalPrice = items.Sum(i => i.TotalPrice),
        ItemCount = items.Sum(i => i.Quantity)
    };
}
