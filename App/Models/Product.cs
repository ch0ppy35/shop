using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "Give the product a name.")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the product description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price
    /// </summary>
    [Range(0, 1_000_000, ErrorMessage = "Price can't be negative.")]
    public decimal Price { get; set; }

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
    [Range(0, int.MaxValue, ErrorMessage = "Stock can't be negative.")]
    public int QuantityInStock { get; set; }

    /// <summary>
    /// Gets or sets the reorder threshold
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Reorder point can't be negative.")]
    public int ReorderThreshold { get; set; }
}