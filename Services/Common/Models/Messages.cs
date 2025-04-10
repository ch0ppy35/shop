namespace Common.Models;

/// <summary>
/// Base message for all NATS messages
/// </summary>
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
}

/// <summary>
/// Message for product-related operations
/// </summary>
public class ProductMessage : BaseMessage
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
    /// Gets or sets the operation type
    /// </summary>
    public ProductOperationType OperationType { get; set; }
}

/// <summary>
/// Operation types for product messages
/// </summary>
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
    GetAll
}

/// <summary>
/// Message for order-related operations
/// </summary>
public class OrderMessage : BaseMessage
{
    /// <summary>
    /// Gets or sets the order ID
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the customer ID
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the order items
    /// </summary>
    public List<OrderItem>? Items { get; set; }

    /// <summary>
    /// Gets or sets the order status
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the operation type
    /// </summary>
    public OrderOperationType OperationType { get; set; }
}

/// <summary>
/// Order item
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets the product ID
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price
    /// </summary>
    public decimal Price { get; set; }
}

/// <summary>
/// Order status
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order created
    /// </summary>
    Created,

    /// <summary>
    /// Order processed
    /// </summary>
    Processed,

    /// <summary>
    /// Order shipped
    /// </summary>
    Shipped,

    /// <summary>
    /// Order delivered
    /// </summary>
    Delivered,

    /// <summary>
    /// Order cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Operation types for order messages
/// </summary>
public enum OrderOperationType
{
    /// <summary>
    /// Create a new order
    /// </summary>
    Create,

    /// <summary>
    /// Update an existing order
    /// </summary>
    Update,

    /// <summary>
    /// Delete an order
    /// </summary>
    Delete,

    /// <summary>
    /// Get an order
    /// </summary>
    Get,

    /// <summary>
    /// Get all orders
    /// </summary>
    GetAll
}
