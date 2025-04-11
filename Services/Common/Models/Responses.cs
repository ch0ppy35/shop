namespace Common.Models;

/// <summary>
/// Base response for all API responses
/// </summary>
[Serializable]
public class BaseResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the error message
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the session ID for tracking requests across services
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// Response for product operations
/// </summary>
[Serializable]
public class ProductResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the product
    /// </summary>
    public ProductMessage? Product { get; set; }
}

/// <summary>
/// Response for get all products operation
/// </summary>
[Serializable]
public class ProductListResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the products
    /// </summary>
    public List<ProductMessage>? Products { get; set; }
}


