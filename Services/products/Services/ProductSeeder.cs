using Common.Database;
using Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Products.Services;

/// <summary>
/// Service for seeding the products database
/// </summary>
public class ProductSeeder
{
    private readonly ILogger<ProductSeeder> _logger;
    private readonly ProductDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductSeeder"/> class.
    /// </summary>
    public ProductSeeder(ILogger<ProductSeeder> logger, ProductDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Seeds the database with sample products
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Check if there's already data
            if (await _dbContext.Products.AnyAsync())
            {
                _logger.LogInformation("Products table already has data, skipping seed");
                return;
            }

            _logger.LogInformation("Seeding products table with initial data");

            // Create sample products
            var products = new List<ProductEntity>
            {
                new ProductEntity
                {
                    ProductId = "prod-001",
                    Name = "Ergonomic Keyboard",
                    Description = "Comfortable keyboard for long typing sessions",
                    Price = 89.99m,
                    Quantity = 100,
                    Sku = "KB-ERG-001",
                    Location = "Warehouse A",
                    QuantityInStock = 50,
                    ReorderThreshold = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-002",
                    Name = "Wireless Mouse",
                    Description = "High-precision wireless mouse",
                    Price = 49.99m,
                    Quantity = 150,
                    Sku = "MS-WRL-002",
                    Location = "Warehouse A",
                    QuantityInStock = 75,
                    ReorderThreshold = 15,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-003",
                    Name = "Ultra-wide Monitor",
                    Description = "34-inch curved ultra-wide monitor",
                    Price = 399.99m,
                    Quantity = 30,
                    Sku = "MN-UW-003",
                    Location = "Warehouse B",
                    QuantityInStock = 15,
                    ReorderThreshold = 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-004",
                    Name = "Mechanical Keyboard",
                    Description = "Tactile mechanical keyboard with RGB lighting",
                    Price = 129.99m,
                    Quantity = 80,
                    Sku = "KB-MCH-004",
                    Location = "Warehouse A",
                    QuantityInStock = 40,
                    ReorderThreshold = 8,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-005",
                    Name = "Laptop Stand",
                    Description = "Adjustable aluminum laptop stand",
                    Price = 39.99m,
                    Quantity = 200,
                    Sku = "ACC-STD-005",
                    Location = "Warehouse C",
                    QuantityInStock = 100,
                    ReorderThreshold = 20,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-006",
                    Name = "USB-C Hub",
                    Description = "7-in-1 USB-C hub with HDMI and card readers",
                    Price = 59.99m,
                    Quantity = 120,
                    Sku = "ACC-HUB-006",
                    Location = "Warehouse C",
                    QuantityInStock = 60,
                    ReorderThreshold = 12,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-007",
                    Name = "Wireless Headphones",
                    Description = "Noise-cancelling wireless headphones",
                    Price = 199.99m,
                    Quantity = 50,
                    Sku = "AUD-WH-007",
                    Location = "Warehouse B",
                    QuantityInStock = 25,
                    ReorderThreshold = 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-008",
                    Name = "Webcam",
                    Description = "4K webcam with microphone",
                    Price = 79.99m,
                    Quantity = 90,
                    Sku = "CAM-WEB-008",
                    Location = "Warehouse B",
                    QuantityInStock = 45,
                    ReorderThreshold = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-009",
                    Name = "External SSD",
                    Description = "1TB portable SSD drive",
                    Price = 149.99m,
                    Quantity = 60,
                    Sku = "STR-SSD-009",
                    Location = "Warehouse D",
                    QuantityInStock = 30,
                    ReorderThreshold = 6,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = "prod-010",
                    Name = "Gaming Mouse",
                    Description = "High-DPI gaming mouse with programmable buttons",
                    Price = 69.99m,
                    Quantity = 70,
                    Sku = "MS-GAM-010",
                    Location = "Warehouse A",
                    QuantityInStock = 35,
                    ReorderThreshold = 7,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Add products to database
            await _dbContext.Products.AddRangeAsync(products);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Seed data inserted successfully: {Count} products added", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database: {Message}", ex.Message);
            throw;
        }
    }
}
