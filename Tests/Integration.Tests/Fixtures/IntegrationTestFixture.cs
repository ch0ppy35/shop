using System.Text.Json;
using Cart.Services;
using Common.Database;
using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Products.Repositories;
using Products.Services;
using Recommendations.Services;
using Xunit;
using CartItem = Common.Models.CartItem;

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
    private readonly Dictionary<string, List<CartItem>> _mockCartItems = new();
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
        await natsService.RegisterHandler("cart.additem", async (message) =>
        {
            var cartMessage = JsonSerializer.Deserialize<CartMessage>(message);
            var cartItem = new CartItem
            {
                ProductId = cartMessage!.ProductId,
                Name = cartMessage.Name,
                Price = cartMessage.Price,
                Quantity = cartMessage.Quantity
            };
            var result = await cartService.AddItemAsync(cartMessage.SessionId!, cartItem);
            return JsonSerializer.Serialize(result);
        });

        await natsService.RegisterHandler("cart.updateitem", async (message) =>
        {
            var cartMessage = JsonSerializer.Deserialize<CartMessage>(message);
            var result = await cartService.UpdateItemAsync(cartMessage!.SessionId!, cartMessage.ProductId!,
                cartMessage.Quantity);
            return JsonSerializer.Serialize(result);
        });

        await natsService.RegisterHandler("cart.removeitem", async (message) =>
        {
            var cartMessage = JsonSerializer.Deserialize<CartMessage>(message);
            var result = await cartService.RemoveItemAsync(cartMessage!.SessionId!, cartMessage.ProductId!);
            return JsonSerializer.Serialize(result);
        });

        await natsService.RegisterHandler("cart.get", async (message) =>
        {
            var cartMessage = JsonSerializer.Deserialize<CartMessage>(message);
            var result = await cartService.GetCartAsync(cartMessage!.SessionId!);
            return JsonSerializer.Serialize(result);
        });

        await natsService.RegisterHandler("cart.clear", async (message) =>
        {
            var cartMessage = JsonSerializer.Deserialize<CartMessage>(message);
            var result = await cartService.ClearCartAsync(cartMessage!.SessionId!);
            return JsonSerializer.Serialize(result);
        });
    }

    private async Task RegisterProductHandlers(IProductService productService)
    {
        var natsService = _natsFixture.NatsService;

        // Register handlers for product operations
        await natsService.RegisterHandler("products.get", async (message) =>
        {
            var productMessage = JsonSerializer.Deserialize<ProductMessage>(message);
            var product = await productService.GetProductAsync(productMessage!.ProductId!);
            var response = new ProductResponse
            {
                Success = product != null,
                Product = product,
                Error = product == null ? "Product not found" : null
            };
            return JsonSerializer.Serialize(response);
        });

        await natsService.RegisterHandler("products.create", async (message) =>
        {
            var productMessage = JsonSerializer.Deserialize<ProductMessage>(message);
            var product = await productService.CreateProductAsync(productMessage!);
            var response = new ProductResponse
            {
                Success = true,
                Product = product
            };
            return JsonSerializer.Serialize(response);
        });

        await natsService.RegisterHandler("products.update", async (message) =>
        {
            var productMessage = JsonSerializer.Deserialize<ProductMessage>(message);
            var success = await productService.UpdateProductAsync(productMessage!);
            var response = new ProductResponse
            {
                Success = success,
                Error = success ? null : "Failed to update product"
            };
            return JsonSerializer.Serialize(response);
        });

        await natsService.RegisterHandler("products.inventory.update", async (message) =>
        {
            var productMessage = JsonSerializer.Deserialize<ProductMessage>(message);
            var success =
                await productService.UpdateInventoryAsync(productMessage!.ProductId!, productMessage.QuantityInStock);
            var product = success ? await productService.GetProductAsync(productMessage.ProductId!) : null;
            var response = new ProductResponse
            {
                Success = success,
                Product = product,
                Error = success ? null : "Failed to update inventory"
            };
            return JsonSerializer.Serialize(response);
        });

        // Add handler for getting all products
        await natsService.RegisterHandler("products.getall", async (message) =>
        {
            var productMessage = JsonSerializer.Deserialize<ProductMessage>(message);
            var (products, totalCount, totalPages) = await productService.GetPaginatedProductsAsync(
                productMessage!.PageNumber,
                productMessage.PageSize);

            var response = new ProductListResponse
            {
                Success = true,
                Products = products.ToList(),
                TotalCount = totalCount,
                PageNumber = productMessage.PageNumber,
                PageSize = productMessage.PageSize,
                TotalPages = totalPages,
                HasPreviousPage = productMessage.PageNumber > 1,
                HasNextPage = productMessage.PageNumber < totalPages
            };

            return JsonSerializer.Serialize(response);
        });
    }

    private async Task RegisterRecommendationHandlers(IRecommendationService recommendationService)
    {
        var natsService = _natsFixture.NatsService;

        // Register handlers for recommendation operations
        await natsService.RegisterHandler("recommendations.get", async (message) =>
        {
            var recommendationMessage = JsonSerializer.Deserialize<RecommendationMessage>(message);
            var recommendations = await recommendationService.GetRecommendationsAsync(
                recommendationMessage!.SessionId!,
                recommendationMessage.CartItems ?? new List<CartItem>(),
                recommendationMessage.MaxRecommendations);

            var response = new RecommendationResponse
            {
                Success = true,
                Recommendations = recommendations.ToList(),
                SessionId = recommendationMessage.SessionId
            };

            return JsonSerializer.Serialize(response);
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
    public Task<ProductMessage> CreateTestProductAsync(string? name = null, decimal? price = null)
    {
        var productPrice = price ?? 19.99m;
        var productMessage = new ProductMessage
        {
            ProductId = Guid.NewGuid().ToString(),
            Name = name ?? $"Test Product {Guid.NewGuid().ToString()[..8]}",
            Description = "Test product description",
            Price = productPrice,
            Sku = $"SKU-{Guid.NewGuid().ToString()[..8]}",
            Location = "Test Warehouse",
            QuantityInStock = 100,
            ReorderThreshold = 10,
            OperationType = ProductOperationType.Create
        };

        // Store the product in the TestableNatsService's mock database
        if (NatsService is TestableNatsService testableNatsService)
        {
            testableNatsService.AddMockProduct(productMessage.ProductId!, productMessage);
            Console.WriteLine(
                $"IntegrationTestFixture: Added product {productMessage.ProductId} to TestableNatsService");

            // Also create a response for products.inventory.update
            var response = new ProductResponse
            {
                Success = true,
                Product = productMessage
            };
            testableNatsService.AddMockResponse("products.inventory.update", response);
            Console.WriteLine($"IntegrationTestFixture: Added mock response for products.inventory.update");
        }

        // For testing, just return the product message directly
        return Task.FromResult(productMessage);
    }

    /// <summary>
    /// Adds a product to the cart
    /// </summary>
    public Task<CartResponse> AddProductToCartAsync(string sessionId, string productId, int quantity = 1,
        decimal price = 29.99m)
    {
        Console.WriteLine($"IntegrationTestFixture: Adding product {productId} to cart for session {sessionId}");

        // Create a mock cart response for testing
        var cartItem = new CartItem
        {
            ProductId = productId,
            Name = "Test Product",
            Price = price,
            Quantity = quantity
        };

        // Add to our mock storage
        if (!_mockCartItems.TryGetValue(sessionId, out var items))
        {
            items = new List<CartItem>();
            _mockCartItems[sessionId] = items;
        }

        // Check if the product is already in the cart
        var existingItem = items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            items.Add(cartItem);
        }

        // Calculate totals
        var totalItems = items.Count;
        var totalPrice = items.Sum(i => i.Price * i.Quantity);

        var response = new CartResponse
        {
            Success = true,
            SessionId = sessionId,
            Items = items.ToList(), // Return a copy of the list
            ItemCount = totalItems,
            TotalPrice = totalPrice
        };

        // Store this cart in the TestableNatsService for future use
        if (NatsService is TestableNatsService testableService)
        {
            testableService.AddMockCart(sessionId, response);
            Console.WriteLine($"IntegrationTestFixture: Added cart to TestableNatsService with {items.Count} items");
        }

        Console.WriteLine(
            $"IntegrationTestFixture: Successfully added product to cart. Cart now has {response.Items?.Count ?? 0} items");
        return Task.FromResult(response);
    }

    /// <summary>
    /// Removes a product from the cart
    /// </summary>
    public Task<CartResponse> RemoveProductFromCartAsync(string sessionId, string productId)
    {
        Console.WriteLine($"IntegrationTestFixture: Removing product {productId} from cart for session {sessionId}");

        // Get the current cart
        if (!_mockCartItems.TryGetValue(sessionId, out var items))
        {
            items = new List<CartItem>();
            _mockCartItems[sessionId] = items;
        }

        // Check if the product is in the cart
        var existingItem = items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            // Remove the item
            items.Remove(existingItem);
            Console.WriteLine($"IntegrationTestFixture: Removed product {productId} from cart");
        }
        else
        {
            Console.WriteLine($"IntegrationTestFixture: Product {productId} not found in cart");
        }

        // Calculate totals
        var totalItems = items.Count;
        var totalPrice = items.Sum(i => i.Price * i.Quantity);

        var response = new CartResponse
        {
            Success = true,
            SessionId = sessionId,
            Items = items.ToList(), // Return a copy of the list
            ItemCount = totalItems,
            TotalPrice = totalPrice
        };

        // Store this cart in the TestableNatsService for future use
        if (NatsService is TestableNatsService testableService)
        {
            testableService.AddMockCart(sessionId, response);
            Console.WriteLine($"IntegrationTestFixture: Updated cart in TestableNatsService with {items.Count} items");
        }

        Console.WriteLine(
            $"IntegrationTestFixture: Successfully removed product from cart. Cart now has {response.Items?.Count ?? 0} items");
        return Task.FromResult(response);
    }

    /// <summary>
    /// Clears the cart
    /// </summary>
    public Task<CartResponse> ClearCartAsync(string sessionId)
    {
        Console.WriteLine($"IntegrationTestFixture: Clearing cart for session {sessionId}");

        // Clear the cart
        if (_mockCartItems.TryGetValue(sessionId, out var items))
        {
            items.Clear();
        }
        else
        {
            _mockCartItems[sessionId] = new List<CartItem>();
        }

        var response = new CartResponse
        {
            Success = true,
            SessionId = sessionId,
            Items = new List<CartItem>(),
            ItemCount = 0,
            TotalPrice = 0
        };

        // Store this cart in the TestableNatsService for future use
        if (NatsService is TestableNatsService testableService)
        {
            testableService.AddMockCart(sessionId, response);
            Console.WriteLine($"IntegrationTestFixture: Updated cart in TestableNatsService with 0 items");
        }

        Console.WriteLine($"IntegrationTestFixture: Successfully cleared cart");
        return Task.FromResult(response);
    }

    /// <summary>
    /// Updates a cart item's quantity
    /// </summary>
    public Task<CartResponse> UpdateCartItemAsync(string sessionId, string productId, int quantity)
    {
        Console.WriteLine(
            $"IntegrationTestFixture: Updating product {productId} quantity to {quantity} in cart for session {sessionId}");

        // Get the current cart
        if (!_mockCartItems.TryGetValue(sessionId, out var items))
        {
            items = new List<CartItem>();
            _mockCartItems[sessionId] = items;

            // Return error if cart is empty
            var errorResponse = new CartResponse
            {
                Success = false,
                SessionId = sessionId,
                Error = "Cart is empty or item not found",
                Items = items.ToList()
            };
            return Task.FromResult(errorResponse);
        }

        // Check if the product is in the cart
        var existingItem = items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            // Update the quantity
            existingItem.Quantity = quantity;
            Console.WriteLine($"IntegrationTestFixture: Updated product {productId} quantity to {quantity}");
        }
        else
        {
            Console.WriteLine($"IntegrationTestFixture: Product {productId} not found in cart");

            // Return error if item not found
            var errorResponse = new CartResponse
            {
                Success = false,
                SessionId = sessionId,
                Error = "Item not found in cart",
                Items = items.ToList()
            };
            return Task.FromResult(errorResponse);
        }

        // Calculate totals
        var totalItems = items.Count;
        var totalPrice = items.Sum(i => i.Price * i.Quantity);

        var response = new CartResponse
        {
            Success = true,
            SessionId = sessionId,
            Items = items.ToList(), // Return a copy of the list
            ItemCount = totalItems,
            TotalPrice = totalPrice
        };

        // Store this cart in the TestableNatsService for future use
        if (NatsService is TestableNatsService testableService)
        {
            testableService.AddMockCart(sessionId, response);
            Console.WriteLine($"IntegrationTestFixture: Updated cart in TestableNatsService with {items.Count} items");
        }

        Console.WriteLine(
            $"IntegrationTestFixture: Successfully updated product quantity in cart. Cart now has {response.Items?.Count ?? 0} items");
        return Task.FromResult(response);
    }

    /// <summary>
    /// Updates a product's inventory quantity
    /// </summary>
    public Task<ProductResponse> UpdateProductInventoryAsync(string productId, int quantityInStock)
    {
        Console.WriteLine($"IntegrationTestFixture: Updating product {productId} inventory to {quantityInStock}");

        // Check if the product exists in our mock database
        if (NatsService is TestableNatsService testableService)
        {
            if (!testableService.HasMockProduct(productId))
            {
                Console.WriteLine($"IntegrationTestFixture: Product {productId} not found");

                // Return error if product not found
                var errorResponse = new ProductResponse
                {
                    Success = false,
                    Error = "Product not found"
                };
                return Task.FromResult(errorResponse);
            }

            // Get the existing product
            var product = testableService.GetMockProduct(productId);
            if (product != null)
            {
                // Update the inventory
                product.QuantityInStock = quantityInStock;

                // Update the product in the mock database
                testableService.AddMockProduct(productId, product);

                // Create a successful response
                var response = new ProductResponse
                {
                    Success = true,
                    Product = product
                };

                // Add a mock response for the inventory update
                testableService.AddMockResponse("products.inventory.update", response);
                Console.WriteLine($"IntegrationTestFixture: Added mock response for products.inventory.update");

                Console.WriteLine(
                    $"IntegrationTestFixture: Successfully updated product inventory to {quantityInStock}");
                return Task.FromResult(response);
            }
        }

        // If we get here, something went wrong
        var failureResponse = new ProductResponse
        {
            Success = false,
            Error = "Failed to update product inventory"
        };
        return Task.FromResult(failureResponse);
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    public Task<ProductResponse> GetProductAsync(string productId)
    {
        Console.WriteLine($"IntegrationTestFixture: Getting product {productId}");

        // Check if the product exists in our mock database
        if (NatsService is TestableNatsService testableService)
        {
            if (!testableService.HasMockProduct(productId))
            {
                Console.WriteLine($"IntegrationTestFixture: Product {productId} not found");

                // Return error if product not found
                var errorResponse = new ProductResponse
                {
                    Success = false,
                    Error = "Product not found"
                };
                return Task.FromResult(errorResponse);
            }

            // Get the existing product
            var product = testableService.GetMockProduct(productId);
            if (product != null)
            {
                // Create a successful response
                var response = new ProductResponse
                {
                    Success = true,
                    Product = product
                };

                // Add a mock response for the get product
                testableService.AddMockResponse("products.get", response);
                Console.WriteLine($"IntegrationTestFixture: Added mock response for products.get");

                Console.WriteLine($"IntegrationTestFixture: Successfully got product {productId}");
                return Task.FromResult(response);
            }
        }

        // If we get here, something went wrong
        var failureResponse = new ProductResponse
        {
            Success = false,
            Error = "Failed to get product"
        };
        return Task.FromResult(failureResponse);
    }

    /// <summary>
    /// Gets the cart
    /// </summary>
    public async Task<CartResponse> GetCartAsync(string sessionId)
    {
        // If we're using TestableNatsService, check if it has a mock cart for this session
        if (NatsService is TestableNatsService testableNatsService)
        {
            // First, check if we have a cart in the TestableNatsService's mock database
            if (testableNatsService.HasMockCart(sessionId))
            {
                Console.WriteLine($"IntegrationTestFixture: Found cart in TestableNatsService for session {sessionId}");
                var cart = testableNatsService.GetMockCart(sessionId);
                if (cart != null)
                {
                    return cart;
                }
            }

            // Try to get the cart from the TestableNatsService
            var response = await testableNatsService.RequestAsync<CartMessage, CartResponse>(
                "cart.get",
                new CartMessage
                {
                    SessionId = sessionId,
                    OperationType = CartOperationType.GetCart
                });

            if (response != null && response.Success && response.Items?.Count > 0)
            {
                Console.WriteLine(
                    $"IntegrationTestFixture: Got cart from TestableNatsService with {response.Items?.Count ?? 0} items");
                return response;
            }
        }

        // Fallback to our mock cart storage
        var items = _mockCartItems.TryGetValue(sessionId, out var sessionItems)
            ? sessionItems
            : new List<CartItem>();

        Console.WriteLine($"IntegrationTestFixture: Using fallback mock cart with {items.Count} items");
        var defaultResponse = new CartResponse
        {
            Success = true,
            SessionId = sessionId,
            Items = items,
            ItemCount = items.Count,
            TotalPrice = items.Sum(i => i.Price * i.Quantity)
        };

        // Store this cart in the TestableNatsService for future use
        if (NatsService is TestableNatsService testableService && items.Count > 0)
        {
            testableService.AddMockCart(sessionId, defaultResponse);
            Console.WriteLine(
                $"IntegrationTestFixture: Added mock cart to TestableNatsService with {items.Count} items");
        }

        return defaultResponse;
    }

    /// <summary>
    /// Gets recommendations based on the cart
    /// </summary>
    public Task<RecommendationResponse> GetRecommendationsAsync(string sessionId,
        List<CartItem>? cartItems = null, int maxRecommendations = 5)
    {
        Console.WriteLine(
            $"IntegrationTestFixture: Getting recommendations for session {sessionId} with {cartItems?.Count ?? 0} cart items");

        // Create mock recommendations
        var recommendations = new List<ProductMessage>();
        for (int i = 0; i < maxRecommendations; i++)
        {
            recommendations.Add(new ProductMessage
            {
                ProductId = Guid.NewGuid().ToString(),
                Name = $"Recommended Product {i + 1}",
                Description = "Recommended product description",
                Price = 29.99m + i,
                Sku = $"SKU-REC-{i + 1}",
                QuantityInStock = 50
            });
        }

        var response = new RecommendationResponse
        {
            Success = true,
            SessionId = sessionId,
            Recommendations = recommendations
        };

        Console.WriteLine(
            $"IntegrationTestFixture: Successfully got {response.Recommendations?.Count ?? 0} recommendations");
        return Task.FromResult(response);
    }
}