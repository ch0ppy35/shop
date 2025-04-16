using Common.Database;
using Common.Database.Models;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests.Fixtures;

/// <summary>
/// Fixture for managing PostgreSQL container for integration tests
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly IServiceCollection _services = new ServiceCollection();
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the database context
    /// </summary>
    public ProductDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Gets the connection string
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresFixture"/> class
    /// </summary>
    public PostgresFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432, true)
            // .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    /// <summary>
    /// Initializes the PostgreSQL container and services
    /// </summary>
    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting PostgreSQL container...");
        // Start the PostgreSQL container
        await _postgresContainer.StartAsync();
        ConnectionString = _postgresContainer.GetConnectionString();
        Console.WriteLine($"PostgreSQL container started with connection string: {ConnectionString}");

        // Configure services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging(builder => builder.AddConsole());

        // Create a real DbContext with the PostgreSQL container
        Console.WriteLine("Creating DbContext with PostgreSQL...");
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: "TestProducts") // Use in-memory for tests
            .Options;

        var logger = _services.BuildServiceProvider().GetRequiredService<ILogger<ProductDbContext>>();
        DbContext = new ProductDbContext(options, logger, configuration);

        // Ensure database is created
        Console.WriteLine("Ensuring database is created...");
        try
        {
            await DbContext.Database.EnsureCreatedAsync();
            Console.WriteLine("Database created successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating database: {ex.Message}");
            throw;
        }

        // Seed the database with test data
        await SeedTestDataAsync(DbContext);

        _services.AddSingleton(DbContext);

        _serviceProvider = _services.BuildServiceProvider();
    }

    /// <summary>
    /// Seeds the database with test data
    /// </summary>
    private async Task SeedTestDataAsync(ProductDbContext dbContext)
    {
        // Add some test products
        var products = new List<ProductEntity>
        {
            new ProductEntity
            {
                ProductId = "11111111-1111-1111-1111-111111111111",
                Name = "Test Product 1",
                Description = "Test Description 1",
                Price = 19.99m,
                Sku = "SKU-001",
                Location = "Test Warehouse",
                QuantityInStock = 100,
                ReorderThreshold = 10
            },
            new ProductEntity
            {
                ProductId = "22222222-2222-2222-2222-222222222222",
                Name = "Test Product 2",
                Description = "Test Description 2",
                Price = 29.99m,
                Sku = "SKU-002",
                Location = "Test Warehouse",
                QuantityInStock = 50,
                ReorderThreshold = 5
            },
            new ProductEntity
            {
                ProductId = "33333333-3333-3333-3333-333333333333",
                Name = "Test Product 3",
                Description = "Test Description 3",
                Price = 39.99m,
                Sku = "SKU-003",
                Location = "Test Warehouse",
                QuantityInStock = 25,
                ReorderThreshold = 5
            }
        };

        await dbContext.Products.AddRangeAsync(products);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Disposes the PostgreSQL container and services
    /// </summary>
    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }
}