namespace Frontend.Models;

/// <summary>
/// Represents an item in the shopping cart
/// </summary>
public class CartItem
{
    /// <summary>
    /// Gets or sets the product ID
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the product price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets the total price for this item
    /// </summary>
    public decimal TotalPrice => Price * Quantity;
}