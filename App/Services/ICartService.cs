using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Interface for cart service
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Gets the current cart
    /// </summary>
    Task<ShoppingCart> GetCartAsync();

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="quantity">The quantity to add</param>
    Task<bool> AddItemAsync(string productId, int quantity = 1);

    /// <summary>
    /// Updates an item in the cart
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="quantity">The new quantity</param>
    Task<bool> UpdateItemAsync(string productId, int quantity);

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    /// <param name="productId">The product ID</param>
    Task<bool> RemoveItemAsync(string productId);

    /// <summary>
    /// Clears the cart
    /// </summary>
    Task<bool> ClearCartAsync();
}
