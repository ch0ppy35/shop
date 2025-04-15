using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Database.Models;

/// <summary>
/// Entity class for products in the database
/// </summary>
public class ProductEntity
{
    /// <summary>
    /// Gets or sets the database ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID (GUID)
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price
    /// </summary>
    public decimal Price { get; set; }


    /// <summary>
    /// Gets or sets the SKU (Stock Keeping Unit)
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the location of the inventory item
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity in stock
    /// </summary>
    public int QuantityInStock { get; set; }

    /// <summary>
    /// Gets or sets the reorder threshold
    /// </summary>
    public int ReorderThreshold { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}