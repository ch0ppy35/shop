using System.Text.Json.Serialization;

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

    /// <summary>
    /// Gets or sets the page number for pagination (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 10;
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

/// <summary>
/// Message for cart-related operations
/// </summary>
[Serializable]
public class CartMessage : BaseMessage
{
    /// <summary>
    /// Gets or sets the cart ID (same as session ID)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CartId { get; set; }

    /// <summary>
    /// Gets or sets the product ID
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the product price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the operation type
    /// </summary>
    public CartOperationType OperationType { get; set; }
}

/// <summary>
/// Operation types for cart messages
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CartOperationType
{
    /// <summary>
    /// Add an item to the cart
    /// </summary>
    AddItem,

    /// <summary>
    /// Update an item in the cart
    /// </summary>
    UpdateItem,

    /// <summary>
    /// Remove an item from the cart
    /// </summary>
    RemoveItem,

    /// <summary>
    /// Get the cart
    /// </summary>
    GetCart,

    /// <summary>
    /// Clear the cart
    /// </summary>
    ClearCart
}

/// <summary>
/// Response for cart operations
/// </summary>
[Serializable]
public class CartResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the cart items
    /// </summary>
    public List<CartItem>? Items { get; set; }

    /// <summary>
    /// Gets or sets the total price of the cart
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Gets or sets the number of items in the cart
    /// </summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// Cart item model
/// </summary>
[Serializable]
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

/// <summary>
/// Message for recommendation-related operations
/// </summary>
[Serializable]
public class RecommendationMessage : BaseMessage
{
    /// <summary>
    /// Gets or sets the cart items to base recommendations on
    /// </summary>
    public List<CartItem>? CartItems { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of recommendations to return
    /// </summary>
    public int MaxRecommendations { get; set; } = 5;

    /// <summary>
    /// Gets or sets the operation type
    /// </summary>
    public RecommendationOperationType OperationType { get; set; }
}

/// <summary>
/// Operation types for recommendation messages
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecommendationOperationType
{
    /// <summary>
    /// Get recommendations based on cart items
    /// </summary>
    GetRecommendations
}