using System.Text.Json.Serialization;

namespace Common.Models;

/// <summary>
/// Kinds of change a product can go through
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProductChangeType
{
    /// <summary>
    /// A product was created
    /// </summary>
    Created,

    /// <summary>
    /// A product's details changed
    /// </summary>
    Updated,

    /// <summary>
    /// A product's stock or location changed
    /// </summary>
    InventoryUpdated,

    /// <summary>
    /// A product was deleted
    /// </summary>
    Deleted
}

/// <summary>
/// Published on <see cref="Messaging.Subjects.ProductsChanged"/> after a product write is saved
/// </summary>
[Serializable]
public class ProductChangedEvent : BaseMessage
{
    /// <summary>
    /// Gets or sets the ID of the product that changed
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets what kind of change it was
    /// </summary>
    public ProductChangeType ChangeType { get; set; }

    /// <summary>
    /// Gets or sets the product as it is now, or null when it was deleted
    /// </summary>
    public ProductMessage? Product { get; set; }
}

/// <summary>
/// Published on <see cref="Messaging.Subjects.CartChanged"/> after a cart write, carrying the whole cart
/// </summary>
[Serializable]
public class CartChangedEvent : BaseMessage
{
    /// <summary>
    /// Gets or sets the cart items
    /// </summary>
    public List<CartItem> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total price of the cart
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Gets or sets the number of items in the cart
    /// </summary>
    public int ItemCount { get; set; }
}
