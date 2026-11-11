using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Talks to the cart API. Every call returns the cart as the server holds it after the call,
/// or null when the request failed, so callers never need a second round trip to refresh.
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Gets the current cart
    /// </summary>
    Task<ShoppingCart?> GetCartAsync();

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    Task<ShoppingCart?> AddItemAsync(string productId, int quantity = 1);

    /// <summary>
    /// Sets the quantity of an item in the cart
    /// </summary>
    Task<ShoppingCart?> UpdateItemAsync(string productId, int quantity);

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    Task<ShoppingCart?> RemoveItemAsync(string productId);

    /// <summary>
    /// Empties the cart
    /// </summary>
    Task<ShoppingCart?> ClearCartAsync();
}
