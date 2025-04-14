namespace Frontend.Models;

/// <summary>
/// Represents a shopping cart
/// </summary>
public class ShoppingCart
{
    /// <summary>
    /// Gets or sets the items in the cart
    /// </summary>
    public List<CartItem> Items { get; set; } = new List<CartItem>();

    /// <summary>
    /// Gets or sets the total price of the cart
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Gets or sets the number of items in the cart
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether the cart is empty
    /// </summary>
    public bool IsEmpty => ItemCount == 0;
}
