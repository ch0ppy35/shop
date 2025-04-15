using Cart.Services;
using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Products.Repositories;
using Products.Services;
using Recommendations.Services;
using Xunit;

namespace Integration.Tests.Fixtures;

/// <summary>
/// Fixture for managing all services for integration tests
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly NatsFixture _natsFixture;
    private readonly PostgresFixture _postgresFixture;
    private readonly RedisFixture _redisFixture;
    private readonly IServiceCollection _services = new ServiceCollection();
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the NATS service
    /// </summary>
    public INatsService NatsService => _natsFixture.NatsService;

    /// <summary>
    /// Gets the product service
    /// </summary>
    public IProductService ProductService => _serviceProvider?.GetRequiredService<IProductService>()
                                             ?? throw new InvalidOperationException("Product service not initialized");

    /// <summary>
    /// Gets the cart service
    /// </summary>
    public CartService CartService => _serviceProvider?.GetRequiredService<CartService>()
                                      ?? throw new InvalidOperationException(
                                          "Cart service not initialized");

    /// <summary>
    /// Gets the recommendation service
    /// </summary>
    public IRecommendationService RecommendationService =>
        _serviceProvider?.GetRequiredService<IRecommendationService>()
        ?? throw new InvalidOperationException("Recommendation service not initialized");

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTestFixture"/> class
    /// </summary>
    public IntegrationTestFixture()
    {
        _natsFixture = new NatsFixture();
        _postgresFixture = new PostgresFixture();
        _redisFixture = new RedisFixture();
    }

    /// <summary>
    /// Initializes all services
    /// </summary>
    public async Task InitializeAsync()
    {
        // Initialize containers
        await _natsFixture.InitializeAsync();
        await _postgresFixture.InitializeAsync();
        await _redisFixture.InitializeAsync();

        // Configure services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nats:Url"] = _natsFixture.NatsUrl,
                ["ConnectionStrings:DefaultConnection"] = _postgresFixture.ConnectionString,
                ["Redis:ConnectionString"] = _redisFixture.ConnectionString
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging(builder => builder.AddConsole());

        // Add NATS service
        _services.AddSingleton<INatsService>(_natsFixture.NatsService);

        // Add Product services
        _services.AddSingleton<IProductDbContext>(_postgresFixture.DbContext);
        _services.AddScoped<IProductRepository, ProductRepository>();
        _services.AddScoped<IProductService, ProductService>();

        // Add Cart services
        _services.AddSingleton<IRedisService>(_redisFixture.RedisService);
        _services.AddScoped<CartService>();

        // Add Recommendation services
        _services.AddScoped<IRecommendationService, RecommendationService>();

        _serviceProvider = _services.BuildServiceProvider();

        // Start the consumer services
        await StartConsumerServicesAsync();
    }

    /// <summary>
    /// Disposes all services
    /// </summary>
    public async Task DisposeAsync()
    {
        await _redisFixture.DisposeAsync();
        await _postgresFixture.DisposeAsync();
        await _natsFixture.DisposeAsync();
    }

    /// <summary>
    /// Starts the consumer services
    /// </summary>
    private async Task StartConsumerServicesAsync()
    {
        // We're using mocks, so we don't need to start any consumer services
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a new session ID for testing
    /// </summary>
    public string CreateTestSessionId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Creates a test product
    /// </summary>
    public async Task<ProductMessage> CreateTestProductAsync(string? name = null, decimal? price = null)
    {
        var productMessage = new ProductMessage
        {
            ProductId = Guid.NewGuid().ToString(),
            Name = name ?? $"Test Product {Guid.NewGuid().ToString()[..8]}",
            Description = "Test product description",
            Price = price ?? 19.99m,
            Sku = $"SKU-{Guid.NewGuid().ToString()[..8]}",
            Location = "Test Warehouse",
            QuantityInStock = 100,
            ReorderThreshold = 10,
            OperationType = ProductOperationType.Create
        };

        var createdProduct = await ProductService.CreateProductAsync(productMessage);
        return createdProduct;
    }

    /// <summary>
    /// Adds a product to the cart
    /// </summary>
    public async Task<CartResponse> AddProductToCartAsync(string sessionId, string productId, int quantity = 1)
    {
        var response = await NatsService.RequestAsync<CartMessage, CartResponse>(
            "cart.additem",
            new CartMessage
            {
                SessionId = sessionId,
                ProductId = productId,
                Quantity = quantity,
                OperationType = CartOperationType.AddItem
            });

        return response ?? throw new InvalidOperationException("Failed to add product to cart");
    }

    /// <summary>
    /// Gets the cart
    /// </summary>
    public async Task<CartResponse> GetCartAsync(string sessionId)
    {
        var response = await NatsService.RequestAsync<CartMessage, CartResponse>(
            "cart.get",
            new CartMessage
            {
                SessionId = sessionId,
                OperationType = CartOperationType.GetCart
            });

        return response ?? throw new InvalidOperationException("Failed to get cart");
    }

    /// <summary>
    /// Gets recommendations based on the cart
    /// </summary>
    public async Task<RecommendationResponse> GetRecommendationsAsync(string sessionId,
        List<CartItem>? cartItems = null, int maxRecommendations = 5)
    {
        var response = await NatsService.RequestAsync<RecommendationMessage, RecommendationResponse>(
            "recommendations.get",
            new RecommendationMessage
            {
                SessionId = sessionId,
                CartItems = cartItems,
                MaxRecommendations = maxRecommendations,
                OperationType = RecommendationOperationType.GetRecommendations
            });

        return response ?? throw new InvalidOperationException("Failed to get recommendations");
    }
}