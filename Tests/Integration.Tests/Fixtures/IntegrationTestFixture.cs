using Cart.Services;
using Common.Database;
using Common.Messaging;
using Common.Models;
using CartItem = Common.Models.CartItem;
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
        Console.WriteLine("IntegrationTestFixture: Initializing services...");
        // Initialize containers
        Console.WriteLine("IntegrationTestFixture: Initializing NATS fixture...");
        await _natsFixture.InitializeAsync();
        Console.WriteLine("IntegrationTestFixture: Initializing PostgreSQL fixture...");
        await _postgresFixture.InitializeAsync();
        Console.WriteLine("IntegrationTestFixture: Initializing Redis fixture...");
        await _redisFixture.InitializeAsync();
        Console.WriteLine("IntegrationTestFixture: All fixtures initialized.");

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

        Console.WriteLine("IntegrationTestFixture: Starting consumer services...");
        // Start the consumer services
        await StartConsumerServicesAsync();
        Console.WriteLine("IntegrationTestFixture: Consumer services started.");
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
        // Register handlers for the services
        var cartService = _serviceProvider!.GetRequiredService<CartService>();
        var productService = _serviceProvider!.GetRequiredService<IProductService>();
        var recommendationService = _serviceProvider!.GetRequiredService<IRecommendationService>();

        // Register cart handlers
        await RegisterCartHandlers(cartService);

        // Register product handlers
        await RegisterProductHandlers(productService);

        // Register recommendation handlers
        await RegisterRecommendationHandlers(recommendationService);

        // Wait a moment for handlers to be registered
        await Task.Delay(500);
    }

    private async Task RegisterCartHandlers(CartService cartService)
    {
        var natsService = _natsFixture.NatsService;

        // Register handlers for cart operations
        natsService.RegisterHandler("cart.additem", async (message) =>
        {
            var cartMessage = System.Text.Json.JsonSerializer.Deserialize<CartMessage>(message);
            var cartItem = new CartItem
            {
                ProductId = cartMessage!.ProductId,
                Name = cartMessage.Name,
                Price = cartMessage.Price,
                Quantity = cartMessage.Quantity
            };
            var result = await cartService.AddItemAsync(cartMessage.SessionId!, cartItem);
            return System.Text.Json.JsonSerializer.Serialize(result);
        });

        natsService.RegisterHandler("cart.updateitem", async (message) =>
        {
            var cartMessage = System.Text.Json.JsonSerializer.Deserialize<CartMessage>(message);
            var result = await cartService.UpdateItemAsync(cartMessage!.SessionId!, cartMessage.ProductId!, cartMessage.Quantity);
            return System.Text.Json.JsonSerializer.Serialize(result);
        });

        natsService.RegisterHandler("cart.removeitem", async (message) =>
        {
            var cartMessage = System.Text.Json.JsonSerializer.Deserialize<CartMessage>(message);
            var result = await cartService.RemoveItemAsync(cartMessage!.SessionId!, cartMessage.ProductId!);
            return System.Text.Json.JsonSerializer.Serialize(result);
        });

        natsService.RegisterHandler("cart.get", async (message) =>
        {
            var cartMessage = System.Text.Json.JsonSerializer.Deserialize<CartMessage>(message);
            var result = await cartService.GetCartAsync(cartMessage!.SessionId!);
            return System.Text.Json.JsonSerializer.Serialize(result);
        });

        natsService.RegisterHandler("cart.clear", async (message) =>
        {
            var cartMessage = System.Text.Json.JsonSerializer.Deserialize<CartMessage>(message);
            var result = await cartService.ClearCartAsync(cartMessage!.SessionId!);
            return System.Text.Json.JsonSerializer.Serialize(result);
        });
    }

    private async Task RegisterProductHandlers(IProductService productService)
    {
        var natsService = _natsFixture.NatsService;

        // Register handlers for product operations
        natsService.RegisterHandler("products.get", async (message) =>
        {
            var productMessage = System.Text.Json.JsonSerializer.Deserialize<ProductMessage>(message);
            var product = await productService.GetProductAsync(productMessage!.ProductId!);
            var response = new ProductResponse
            {
                Success = product != null,
                Product = product,
                Error = product == null ? "Product not found" : null
            };
            return System.Text.Json.JsonSerializer.Serialize(response);
        });

        natsService.RegisterHandler("products.create", async (message) =>
        {
            var productMessage = System.Text.Json.JsonSerializer.Deserialize<ProductMessage>(message);
            var product = await productService.CreateProductAsync(productMessage!);
            var response = new ProductResponse
            {
                Success = true,
                Product = product
            };
            return System.Text.Json.JsonSerializer.Serialize(response);
        });

        natsService.RegisterHandler("products.update", async (message) =>
        {
            var productMessage = System.Text.Json.JsonSerializer.Deserialize<ProductMessage>(message);
            var success = await productService.UpdateProductAsync(productMessage!);
            var response = new ProductResponse
            {
                Success = success,
                Error = success ? null : "Failed to update product"
            };
            return System.Text.Json.JsonSerializer.Serialize(response);
        });

        natsService.RegisterHandler("products.inventory.update", async (message) =>
        {
            var productMessage = System.Text.Json.JsonSerializer.Deserialize<ProductMessage>(message);
            var success = await productService.UpdateInventoryAsync(productMessage!.ProductId!, productMessage.QuantityInStock);
            var product = success ? await productService.GetProductAsync(productMessage.ProductId!) : null;
            var response = new ProductResponse
            {
                Success = success,
                Product = product,
                Error = success ? null : "Failed to update inventory"
            };
            return System.Text.Json.JsonSerializer.Serialize(response);
        });
    }

    private async Task RegisterRecommendationHandlers(IRecommendationService recommendationService)
    {
        var natsService = _natsFixture.NatsService;

        // Register handlers for recommendation operations
        natsService.RegisterHandler("recommendations.get", async (message) =>
        {
            var recommendationMessage = System.Text.Json.JsonSerializer.Deserialize<RecommendationMessage>(message);
            var recommendations = await recommendationService.GetRecommendationsAsync(
                recommendationMessage!.SessionId!,
                recommendationMessage.CartItems,
                recommendationMessage.MaxRecommendations);

            var response = new RecommendationResponse
            {
                Success = true,
                Recommendations = recommendations.ToList(),
                SessionId = recommendationMessage.SessionId
            };

            return System.Text.Json.JsonSerializer.Serialize(response);
        });
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
        Console.WriteLine($"IntegrationTestFixture: Adding product {productId} to cart for session {sessionId}");
        var response = await NatsService.RequestAsync<CartMessage, CartResponse>(
            "cart.additem",
            new CartMessage
            {
                SessionId = sessionId,
                ProductId = productId,
                Quantity = quantity,
                OperationType = CartOperationType.AddItem
            });

        if (response == null)
        {
            Console.WriteLine("IntegrationTestFixture: Failed to add product to cart - response was null");
            throw new InvalidOperationException("Failed to add product to cart");
        }

        Console.WriteLine($"IntegrationTestFixture: Successfully added product to cart. Cart now has {response.Items?.Count ?? 0} items");
        return response;
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
        Console.WriteLine($"IntegrationTestFixture: Getting recommendations for session {sessionId} with {cartItems?.Count ?? 0} cart items");
        var response = await NatsService.RequestAsync<RecommendationMessage, RecommendationResponse>(
            "recommendations.get",
            new RecommendationMessage
            {
                SessionId = sessionId,
                CartItems = cartItems,
                MaxRecommendations = maxRecommendations,
                OperationType = RecommendationOperationType.GetRecommendations
            });

        if (response == null)
        {
            Console.WriteLine("IntegrationTestFixture: Failed to get recommendations - response was null");
            throw new InvalidOperationException("Failed to get recommendations");
        }

        Console.WriteLine($"IntegrationTestFixture: Successfully got {response.Recommendations?.Count ?? 0} recommendations");
        return response;
    }
}