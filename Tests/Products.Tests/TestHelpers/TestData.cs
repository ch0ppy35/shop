using Common.Database.Models;
using Common.Models;

namespace Products.Tests.TestHelpers;

/// <summary>
/// Helper class for creating test data
/// </summary>
public static class TestData
{
    /// <summary>
    /// Creates a list of test product entities
    /// </summary>
    public static List<ProductEntity> GetTestProductEntities(int count = 10)
    {
        var products = new List<ProductEntity>();

        for (int i = 1; i <= count; i++)
        {
            products.Add(new ProductEntity
            {
                Id = i, // Use integer ID for tests
                ProductId = $"test-product-{i}",
                Name = $"Test Product {i}",
                Description = $"Description for test product {i}",
                Price = 10.99m + i,
                Quantity = 100 + i,
                Sku = $"SKU-{i}",
                Location = $"Warehouse {(i % 3) + 1}",
                QuantityInStock = 50 + i,
                ReorderThreshold = 10,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            });
        }

        return products;
    }

    /// <summary>
    /// Creates a test product entity
    /// </summary>
    public static ProductEntity GetTestProductEntity(string productId = "test-product-1")
    {
        return new ProductEntity
        {
            Id = 1, // Use integer ID for tests
            ProductId = productId,
            Name = "Test Product",
            Description = "Description for test product",
            Price = 19.99m,
            Quantity = 100,
            Sku = "SKU-TEST",
            Location = "Warehouse A",
            QuantityInStock = 50,
            ReorderThreshold = 10,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    /// <summary>
    /// Creates a test product message
    /// </summary>
    public static ProductMessage GetTestProductMessage(string productId = "test-product-1")
    {
        return new ProductMessage
        {
            ProductId = productId,
            Name = "Test Product",
            Description = "Description for test product",
            Price = 19.99m,
            Quantity = 100,
            Sku = "SKU-TEST",
            Location = "Warehouse A",
            QuantityInStock = 50,
            ReorderThreshold = 10,
            OperationType = ProductOperationType.Create
        };
    }
}
