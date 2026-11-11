using Common.Models;

namespace Cart.Services;

/// <summary>
/// Stores carts. Every write is atomic and returns the cart as it is right after that write.
/// </summary>
public interface ICartStore
{
    /// <summary>
    /// Gets the items in a cart, or an empty list when the cart doesn't exist
    /// </summary>
    Task<List<CartItem>> GetItemsAsync(string sessionId);

    /// <summary>
    /// Adds <paramref name="item"/>'s quantity to the cart, creating the line if needed
    /// </summary>
    Task<List<CartItem>> AddItemAsync(string sessionId, CartItem item);

    /// <summary>
    /// Sets a line's quantity, removing it when <paramref name="quantity"/> is 0 or less
    /// </summary>
    /// <returns>The cart, or null when the product isn't in it</returns>
    Task<List<CartItem>?> SetQuantityAsync(string sessionId, string productId, int quantity);

    /// <summary>
    /// Removes a line from the cart
    /// </summary>
    /// <returns>The cart, or null when the product isn't in it</returns>
    Task<List<CartItem>?> RemoveItemAsync(string sessionId, string productId);

    /// <summary>
    /// Deletes the cart
    /// </summary>
    Task ClearAsync(string sessionId);
}
