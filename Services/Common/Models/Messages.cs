using System.Text.Json.Serialization;
using System.Text.Json;


namespace Common.Models;

/// <summary>
/// Base message for all NATS messages
/// </summary>
[Serializable]
public abstract class BaseMessage
{
    /// <summary>
    /// Gets or sets the message ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the session ID for tracking requests across services
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the reply-to subject for request-reply pattern
    /// </summary>
    [JsonIgnore] // This is set by NATS, not serialized in the message
    public string? ReplyTo { get; set; }
}

/// <summary>
/// Message for product-related operations
/// </summary>
[Serializable]
public class ProductMessage : BaseMessage
{
    /// <summary>
    /// Gets or sets the product ID
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductId { get; set; }

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

    /// <summary>
    /// Gets or sets the operation type
    /// </summary>
    public ProductOperationType OperationType { get; set; }
}

/// <summary>
/// Operation types for product messages
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProductOperationType
{
    /// <summary>
    /// Create a new product
    /// </summary>
    Create,

    /// <summary>
    /// Update an existing product
    /// </summary>
    Update,

    /// <summary>
    /// Delete a product
    /// </summary>
    Delete,

    /// <summary>
    /// Get a product
    /// </summary>
    Get,

    /// <summary>
    /// Get all products
    /// </summary>
    GetAll,

    /// <summary>
    /// Get inventory for a product
    /// </summary>
    GetInventory,

    /// <summary>
    /// Update inventory for a product
    /// </summary>
    UpdateInventory
}
