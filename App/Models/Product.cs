namespace Frontend.Models;

/// <summary>
/// Represents a product in the shop
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the product ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the product description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the SKU (Stock Keeping Unit)
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the location of the inventory item
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the quantity in stock
    /// </summary>
    public int QuantityInStock { get; set; }

    /// <summary>
    /// Gets or sets the reorder threshold
    /// </summary>
    public int ReorderThreshold { get; set; }
}
