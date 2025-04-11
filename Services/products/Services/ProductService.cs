using Common.Models;
using Microsoft.Extensions.Logging;
using Products.Repositories;

namespace Products.Services;

/// <summary>
/// Service for managing products
/// </summary>
public class ProductService
{
    private readonly ILogger<ProductService> _logger;
    private readonly ProductRepository _productRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class.
    /// </summary>
    public ProductService(ILogger<ProductService> logger, ProductRepository productRepository)
    {
        _logger = logger;
        _productRepository = productRepository;

        _logger.LogInformation("ProductService initialized");
    }

    /// <summary>
    /// Gets all products
    /// </summary>
    public async Task<IEnumerable<ProductMessage>> GetAllProductsAsync()
    {
        _logger.LogInformation("Getting all products from database");
        var products = await _productRepository.GetAllProductsAsync();
        return products.Select(p => ProductRepository.ToProductMessage(p));
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    public async Task<ProductMessage?> GetProductAsync(string id)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", id);

        var product = await _productRepository.GetProductByIdAsync(id);
        if (product != null)
        {
            return ProductRepository.ToProductMessage(product);
        }

        _logger.LogWarning("Product with ID {ProductId} not found", id);
        return null;
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    public async Task<ProductMessage> CreateProductAsync(ProductMessage product)
    {
        _logger.LogInformation("Creating new product with ID: {ProductId}", product.ProductId);

        if (string.IsNullOrEmpty(product.ProductId))
        {
            product.ProductId = Guid.NewGuid().ToString();
        }

        var entity = ProductRepository.ToProductEntity(product);
        var createdProduct = await _productRepository.CreateProductAsync(entity);

        return ProductRepository.ToProductMessage(createdProduct, ProductOperationType.Create);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    public async Task<bool> UpdateProductAsync(ProductMessage product)
    {
        if (string.IsNullOrEmpty(product.ProductId))
        {
            _logger.LogWarning("Cannot update product with null or empty ID");
            return false;
        }

        _logger.LogInformation("Updating product with ID: {ProductId}", product.ProductId);

        var entity = ProductRepository.ToProductEntity(product);
        return await _productRepository.UpdateProductAsync(entity);
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    public async Task<bool> DeleteProductAsync(string id)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);
        return await _productRepository.DeleteProductAsync(id);
    }
}
