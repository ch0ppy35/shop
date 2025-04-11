using Common.Database;
using Common.Database.Models;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Products.Repositories;

/// <summary>
/// Repository for product data
/// </summary>
public class ProductRepository
{
    private readonly ILogger<ProductRepository> _logger;
    private readonly ProductDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductRepository"/> class.
    /// </summary>
    public ProductRepository(ILogger<ProductRepository> logger, ProductDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets all products
    /// </summary>
    public async Task<IEnumerable<ProductEntity>> GetAllProductsAsync()
    {
        _logger.LogInformation("Getting all products from database");

        var products = await _dbContext.Products.ToListAsync();

        // Log the first product to debug
        var firstProduct = products.FirstOrDefault();
        if (firstProduct != null)
        {
            _logger.LogInformation("First product: ID={Id}, ProductId={ProductId}, Name={Name}",
                firstProduct.Id, firstProduct.ProductId, firstProduct.Name);
        }

        return products;
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    public async Task<ProductEntity?> GetProductByIdAsync(string productId)
    {
        _logger.LogInformation("Getting product with ID: {ProductId} from database", productId);

        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    public async Task<ProductEntity> CreateProductAsync(ProductEntity product)
    {
        _logger.LogInformation("Creating new product with ID: {ProductId} in database", product.ProductId);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return product;
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    public async Task<bool> UpdateProductAsync(ProductEntity product)
    {
        _logger.LogInformation("Updating product with ID: {ProductId} in database", product.ProductId);

        var existingProduct = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

        if (existingProduct == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found for update", product.ProductId);
            return false;
        }

        // Update properties
        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.Quantity = product.Quantity;
        existingProduct.Sku = product.Sku;
        existingProduct.Location = product.Location;
        existingProduct.QuantityInStock = product.QuantityInStock;
        existingProduct.ReorderThreshold = product.ReorderThreshold;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    public async Task<bool> DeleteProductAsync(string productId)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId} from database", productId);

        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found for deletion", productId);
            return false;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Converts a ProductEntity to a ProductMessage
    /// </summary>
    public static ProductMessage ToProductMessage(ProductEntity entity, ProductOperationType operationType = ProductOperationType.Get)
    {
        // Ensure the ProductId is not null or empty
        var productId = !string.IsNullOrEmpty(entity.ProductId) ? entity.ProductId : Guid.NewGuid().ToString();

        return new ProductMessage
        {
            ProductId = productId,
            Name = entity.Name,
            Description = entity.Description,
            Price = entity.Price,
            Quantity = entity.Quantity,
            Sku = entity.Sku,
            Location = entity.Location,
            QuantityInStock = entity.QuantityInStock,
            ReorderThreshold = entity.ReorderThreshold,
            OperationType = operationType
        };
    }

    /// <summary>
    /// Converts a ProductMessage to a ProductEntity
    /// </summary>
    public static ProductEntity ToProductEntity(ProductMessage message)
    {
        return new ProductEntity
        {
            ProductId = message.ProductId ?? Guid.NewGuid().ToString(),
            Name = message.Name ?? string.Empty,
            Description = message.Description,
            Price = message.Price,
            Quantity = message.Quantity,
            Sku = message.Sku ?? string.Empty,
            Location = message.Location ?? string.Empty,
            QuantityInStock = message.QuantityInStock,
            ReorderThreshold = message.ReorderThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
