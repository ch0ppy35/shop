using Common.Database;
using Common.Database.Models;
using Microsoft.EntityFrameworkCore;

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
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Ergonomic Keyboard",
                    Description = "Comfortable keyboard for long typing sessions",
                    Price = 89.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "KB-ERG-001",
                    Location = "Warehouse A",
                    QuantityInStock = 50,
                    ReorderThreshold = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Wireless Mouse",
                    Description = "High-precision wireless mouse",
                    Price = 49.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "MS-WRL-002",
                    Location = "Warehouse A",
                    QuantityInStock = 75,
                    ReorderThreshold = 15,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Ultra-wide Monitor",
                    Description = "34-inch curved ultra-wide monitor",
                    Price = 399.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "MN-UW-003",
                    Location = "Warehouse B",
                    QuantityInStock = 15,
                    ReorderThreshold = 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Mechanical Keyboard",
                    Description = "Tactile mechanical keyboard with RGB lighting",
                    Price = 129.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "KB-MCH-004",
                    Location = "Warehouse A",
                    QuantityInStock = 40,
                    ReorderThreshold = 8,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Laptop Stand",
                    Description = "Adjustable aluminum laptop stand",
                    Price = 39.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "ACC-STD-005",
                    Location = "Warehouse C",
                    QuantityInStock = 100,
                    ReorderThreshold = 20,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "USB-C Hub",
                    Description = "7-in-1 USB-C hub with HDMI and card readers",
                    Price = 59.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "ACC-HUB-006",
                    Location = "Warehouse C",
                    QuantityInStock = 60,
                    ReorderThreshold = 12,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Wireless Headphones",
                    Description = "Noise-cancelling wireless headphones",
                    Price = 199.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "AUD-WH-007",
                    Location = "Warehouse B",
                    QuantityInStock = 25,
                    ReorderThreshold = 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Webcam",
                    Description = "4K webcam with microphone",
                    Price = 79.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "CAM-WEB-008",
                    Location = "Warehouse B",
                    QuantityInStock = 45,
                    ReorderThreshold = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "External SSD",
                    Description = "1TB portable SSD drive",
                    Price = 149.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "STR-SSD-009",
                    Location = "Warehouse D",
                    QuantityInStock = 30,
                    ReorderThreshold = 6,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Gaming Mouse",
                    Description = "High-DPI gaming mouse with programmable buttons",
                    Price = 69.99m,
                    // Quantity field removed - using QuantityInStock instead
                    Sku = "MS-GAM-010",
                    Location = "Warehouse A",
                    QuantityInStock = 35,
                    ReorderThreshold = 7,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "HDMI Cable",
                    Description = "6ft HDMI 2.0 cable",
                    Price = 9.99m,
                    Sku = "CBL-HDM-011",
                    Location = "Warehouse C",
                    QuantityInStock = 200,
                    ReorderThreshold = 40,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "USB-C to USB Adapter",
                    Description = "Compact USB-C to USB-A adapter",
                    Price = 6.99m,
                    Sku = "ADA-USBC-012",
                    Location = "Warehouse A",
                    QuantityInStock = 180,
                    ReorderThreshold = 30,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Ethernet Cable",
                    Description = "10ft Cat6 ethernet cable",
                    Price = 7.99m,
                    Sku = "CBL-ETH-013",
                    Location = "Warehouse B",
                    QuantityInStock = 150,
                    ReorderThreshold = 25,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Lightning Charging Cable",
                    Description = "3ft Apple lightning cable",
                    Price = 12.99m,
                    Sku = "CBL-LTG-014",
                    Location = "Warehouse A",
                    QuantityInStock = 120,
                    ReorderThreshold = 20,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "USB Wall Charger",
                    Description = "Dual port USB wall charger",
                    Price = 14.99m,
                    Sku = "CHG-WALL-015",
                    Location = "Warehouse C",
                    QuantityInStock = 90,
                    ReorderThreshold = 18,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Cable Organizer",
                    Description = "Magnetic cable clips for desks",
                    Price = 5.99m,
                    Sku = "ORG-CBL-016",
                    Location = "Warehouse B",
                    QuantityInStock = 75,
                    ReorderThreshold = 15,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Mouse Pad",
                    Description = "Standard black mouse pad",
                    Price = 4.99m,
                    Sku = "ACC-MPAD-017",
                    Location = "Warehouse D",
                    QuantityInStock = 100,
                    ReorderThreshold = 20,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Micro USB Cable",
                    Description = "3ft micro USB charging cable",
                    Price = 6.49m,
                    Sku = "CBL-MUSB-018",
                    Location = "Warehouse A",
                    QuantityInStock = 140,
                    ReorderThreshold = 28,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "USB Extension Cable",
                    Description = "6ft USB 3.0 extension cable",
                    Price = 8.99m,
                    Sku = "CBL-EXT-019",
                    Location = "Warehouse B",
                    QuantityInStock = 85,
                    ReorderThreshold = 17,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "3.5mm Audio Cable",
                    Description = "6ft male-to-male auxiliary audio cable",
                    Price = 7.49m,
                    Sku = "CBL-AUX-020",
                    Location = "Warehouse C",
                    QuantityInStock = 110,
                    ReorderThreshold = 22,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "USB-C Charging Cable",
                    Description = "6ft USB-C to USB-C charging cable",
                    Price = 10.99m,
                    Sku = "CBL-USBC-021",
                    Location = "Warehouse A",
                    QuantityInStock = 130,
                    ReorderThreshold = 26,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Screen Cleaning Kit",
                    Description = "Screen-safe cleaning spray and microfiber cloth",
                    Price = 9.49m,
                    Sku = "CLN-SCR-022",
                    Location = "Warehouse B",
                    QuantityInStock = 95,
                    ReorderThreshold = 19,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Power Strip",
                    Description = "6-outlet power strip with surge protection",
                    Price = 17.99m,
                    Sku = "PWR-STR-023",
                    Location = "Warehouse D",
                    QuantityInStock = 70,
                    ReorderThreshold = 14,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Laptop Sleeve",
                    Description = "13-inch neoprene laptop sleeve",
                    Price = 15.99m,
                    Sku = "ACC-LPSLV-024",
                    Location = "Warehouse C",
                    QuantityInStock = 60,
                    ReorderThreshold = 12,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Compact Flash Drive",
                    Description = "32GB USB 3.0 flash drive",
                    Price = 11.99m,
                    Sku = "STR-USB-025",
                    Location = "Warehouse A",
                    QuantityInStock = 100,
                    ReorderThreshold = 20,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "SD Card Reader",
                    Description = "USB 3.0 SD card reader",
                    Price = 13.49m,
                    Sku = "ACC-SDCR-026",
                    Location = "Warehouse B",
                    QuantityInStock = 85,
                    ReorderThreshold = 17,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Bluetooth Adapter",
                    Description = "USB Bluetooth 5.0 dongle",
                    Price = 14.49m,
                    Sku = "ADA-BT-027",
                    Location = "Warehouse C",
                    QuantityInStock = 50,
                    ReorderThreshold = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "USB Fan",
                    Description = "Mini USB desk fan",
                    Price = 12.49m,
                    Sku = "ACC-FAN-028",
                    Location = "Warehouse A",
                    QuantityInStock = 45,
                    ReorderThreshold = 9,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Laptop Webcam Cover",
                    Description = "Slideable webcam privacy cover",
                    Price = 3.99m,
                    Sku = "ACC-WBC-029",
                    Location = "Warehouse D",
                    QuantityInStock = 150,
                    ReorderThreshold = 30,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ProductEntity
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = "Velcro Cable Ties",
                    Description = "Reusable cable management ties (pack of 10)",
                    Price = 4.49m,
                    Sku = "ORG-VEL-030",
                    Location = "Warehouse C",
                    QuantityInStock = 160,
                    ReorderThreshold = 32,
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
