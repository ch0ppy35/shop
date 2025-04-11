using Common.Database;
using Common.Database.Models;
using Common.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Products.Repositories;

/// <summary>
/// Repository for product data
/// </summary>
public class ProductRepository
{
    private readonly ILogger<ProductRepository> _logger;
    private readonly DatabaseService _databaseService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductRepository"/> class.
    /// </summary>
    public ProductRepository(ILogger<ProductRepository> logger, DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Gets all products
    /// </summary>
    public async Task<IEnumerable<ProductEntity>> GetAllProductsAsync()
    {
        _logger.LogInformation("Getting all products from database");

        using var connection = _databaseService.CreateConnection();
        var sql = "SELECT * FROM products";

        return await connection.QueryAsync<ProductEntity>(sql);
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    public async Task<ProductEntity?> GetProductByIdAsync(string productId)
    {
        _logger.LogInformation("Getting product with ID: {ProductId} from database", productId);

        using var connection = _databaseService.CreateConnection();
        var sql = "SELECT * FROM products WHERE product_id = @ProductId";

        return await connection.QueryFirstOrDefaultAsync<ProductEntity>(sql, new { ProductId = productId });
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    public async Task<ProductEntity> CreateProductAsync(ProductEntity product)
    {
        _logger.LogInformation("Creating new product with ID: {ProductId} in database", product.ProductId);

        using var connection = _databaseService.CreateConnection();
        var sql = @"
            INSERT INTO products (product_id, name, description, price, quantity, created_at, updated_at)
            VALUES (@ProductId, @Name, @Description, @Price, @Quantity, @CreatedAt, @UpdatedAt)
            RETURNING *";

        return await connection.QueryFirstAsync<ProductEntity>(sql, product);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    public async Task<bool> UpdateProductAsync(ProductEntity product)
    {
        _logger.LogInformation("Updating product with ID: {ProductId} in database", product.ProductId);

        using var connection = _databaseService.CreateConnection();
        var sql = @"
            UPDATE products
            SET name = @Name,
                description = @Description,
                price = @Price,
                quantity = @Quantity,
                updated_at = @UpdatedAt
            WHERE product_id = @ProductId";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            product.ProductId,
            product.Name,
            product.Description,
            product.Price,
            product.Quantity,
            UpdatedAt = DateTime.UtcNow
        });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    public async Task<bool> DeleteProductAsync(string productId)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId} from database", productId);

        using var connection = _databaseService.CreateConnection();
        var sql = "DELETE FROM products WHERE product_id = @ProductId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { ProductId = productId });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Converts a ProductEntity to a ProductMessage
    /// </summary>
    public static ProductMessage ToProductMessage(ProductEntity entity, ProductOperationType operationType = ProductOperationType.Get)
    {
        return new ProductMessage
        {
            ProductId = entity.ProductId,
            Name = entity.Name,
            Description = entity.Description,
            Price = entity.Price,
            Quantity = entity.Quantity,
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
