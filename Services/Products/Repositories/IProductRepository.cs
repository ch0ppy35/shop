using Common.Database.Models;
using Common.Models;

namespace Products.Repositories;

/// <summary>
/// Interface for product repository
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets all products
    /// </summary>
    Task<IEnumerable<ProductEntity>> GetAllProductsAsync();

    /// <summary>
    /// Gets paginated products
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A tuple containing the paginated products and the total count</returns>
    Task<(IEnumerable<ProductEntity> Products, int TotalCount)> GetPaginatedProductsAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    Task<ProductEntity?> GetProductByIdAsync(string productId);

    /// <summary>
    /// Creates a new product
    /// </summary>
    Task<ProductEntity> CreateProductAsync(ProductEntity product);

    /// <summary>
    /// Updates an existing product
    /// </summary>
    Task<bool> UpdateProductAsync(ProductEntity product);

    /// <summary>
    /// Deletes a product
    /// </summary>
    Task<bool> DeleteProductAsync(string productId);
}
