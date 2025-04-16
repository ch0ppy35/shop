using Common.Models;

namespace Products.Services;

/// <summary>
/// Interface for product service
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets all products
    /// </summary>
    Task<IEnumerable<ProductMessage>> GetAllProductsAsync();

    /// <summary>
    /// Gets paginated products
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A tuple containing the paginated products and pagination metadata</returns>
    Task<(IEnumerable<ProductMessage> Products, int TotalCount, int TotalPages)> GetPaginatedProductsAsync(
        int pageNumber, int pageSize);

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    Task<ProductMessage?> GetProductAsync(string id);

    /// <summary>
    /// Creates a new product
    /// </summary>
    Task<ProductMessage> CreateProductAsync(ProductMessage product);

    /// <summary>
    /// Updates an existing product
    /// </summary>
    Task<bool> UpdateProductAsync(ProductMessage product);

    /// <summary>
    /// Deletes a product
    /// </summary>
    Task<bool> DeleteProductAsync(string id);

    /// <summary>
    /// Gets inventory for a product
    /// </summary>
    Task<ProductMessage?> GetInventoryAsync(string id);

    /// <summary>
    /// Updates inventory for a product
    /// </summary>
    Task<bool> UpdateInventoryAsync(string id, int quantity);
}