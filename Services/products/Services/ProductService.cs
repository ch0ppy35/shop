using Common.Models;
using Microsoft.Extensions.Logging;

namespace Products.Services;

/// <summary>
/// Service for managing products
/// </summary>
public class ProductService
{
    private readonly ILogger<ProductService> _logger;
    private readonly Dictionary<string, ProductMessage> _products = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class.
    /// </summary>
    public ProductService(ILogger<ProductService> logger)
    {
        _logger = logger;
        
        // Add some sample products
        var product1 = new ProductMessage
        {
            ProductId = Guid.NewGuid().ToString(),
            Name = "Sample Product 1",
            Description = "This is a sample product",
            Price = 19.99m,
            Quantity = 100,
            OperationType = ProductOperationType.Create
        };
        
        var product2 = new ProductMessage
        {
            ProductId = Guid.NewGuid().ToString(),
            Name = "Sample Product 2",
            Description = "This is another sample product",
            Price = 29.99m,
            Quantity = 50,
            OperationType = ProductOperationType.Create
        };
        
        _products.Add(product1.ProductId!, product1);
        _products.Add(product2.ProductId!, product2);
        
        _logger.LogInformation("ProductService initialized with {Count} sample products", _products.Count);
    }

    /// <summary>
    /// Gets all products
    /// </summary>
    public IEnumerable<ProductMessage> GetAllProducts()
    {
        _logger.LogInformation("Getting all products, count: {Count}", _products.Count);
        return _products.Values;
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    public ProductMessage? GetProduct(string id)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", id);
        
        if (_products.TryGetValue(id, out var product))
        {
            return product;
        }
        
        _logger.LogWarning("Product with ID {ProductId} not found", id);
        return null;
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    public ProductMessage CreateProduct(ProductMessage product)
    {
        _logger.LogInformation("Creating new product with ID: {ProductId}", product.ProductId);
        
        if (string.IsNullOrEmpty(product.ProductId))
        {
            product.ProductId = Guid.NewGuid().ToString();
        }
        
        _products[product.ProductId] = product;
        return product;
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    public bool UpdateProduct(ProductMessage product)
    {
        if (string.IsNullOrEmpty(product.ProductId))
        {
            _logger.LogWarning("Cannot update product with null or empty ID");
            return false;
        }
        
        _logger.LogInformation("Updating product with ID: {ProductId}", product.ProductId);
        
        if (_products.ContainsKey(product.ProductId))
        {
            _products[product.ProductId] = product;
            return true;
        }
        
        _logger.LogWarning("Product with ID {ProductId} not found for update", product.ProductId);
        return false;
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    public bool DeleteProduct(string id)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);
        
        if (_products.ContainsKey(id))
        {
            _products.Remove(id);
            return true;
        }
        
        _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
        return false;
    }
}
