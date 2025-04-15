using System.Text.Json;
using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Integration.Tests.Fixtures
{
    /// <summary>
    /// A testable version of NatsService that adds handler registration for testing
    /// </summary>
    public class TestableNatsService : NatsService
    {
        private readonly Dictionary<string, Func<string, Task<string>>> _handlers = new();
        private readonly Dictionary<string, object> _mockResponses = new();
        private readonly ILogger<NatsService> _logger;

        /// <summary>
        /// Adds a mock response for a subject
        /// </summary>
        /// <param name="subject">The subject</param>
        /// <param name="response">The response object</param>
        public void AddMockResponse(string subject, object response)
        {
            _mockResponses[subject] = response;
            Console.WriteLine($"TestableNatsService: Added mock response for subject: {subject}");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableNatsService"/> class.
        /// </summary>
        public TestableNatsService(ILogger<NatsService> logger, IConfiguration configuration) : base(logger, configuration)
        {
            _logger = logger;
            InitializeMockResponses();
            RegisterHandlersForAllSubjects();
        }

        /// <summary>
        /// Initializes mock responses for testing
        /// </summary>
        private void InitializeMockResponses()
        {
            // Mock responses for cart operations
            _mockResponses["cart.additem"] = new CartResponse { Success = true, Items = new List<CartItem>() };
            _mockResponses["cart.updateitem"] = new CartResponse { Success = true, Items = new List<CartItem>() };
            _mockResponses["cart.removeitem"] = new CartResponse { Success = true, Items = new List<CartItem>() };
            _mockResponses["cart.get"] = new CartResponse { Success = true, Items = new List<CartItem>() };
            _mockResponses["cart.clear"] = new CartResponse { Success = true, Items = new List<CartItem>() };

            // Mock responses for product operations
            _mockResponses["products.get"] = new ProductResponse { Success = true, Product = new ProductMessage { ProductId = Guid.NewGuid().ToString(), Name = "Test Product", Price = 19.99m, QuantityInStock = 100 } };
            _mockResponses["products.create"] = new ProductResponse { Success = true, Product = new ProductMessage { ProductId = Guid.NewGuid().ToString(), Name = "New Product", Price = 19.99m, QuantityInStock = 100 } };
            _mockResponses["products.update"] = new ProductResponse { Success = true };
            _mockResponses["products.inventory.update"] = new ProductResponse { Success = true, Product = new ProductMessage { ProductId = Guid.NewGuid().ToString(), Name = "Updated Product", Price = 29.99m, QuantityInStock = 75 } };
            _mockResponses["products.getall"] = new ProductListResponse
            {
                Success = true,
                Products = GenerateMockProducts(25),
                TotalCount = 25,
                PageNumber = 1,
                PageSize = 10,
                TotalPages = 3,
                HasNextPage = true,
                HasPreviousPage = false
            };

            // Mock responses for recommendation operations
            _mockResponses["recommendations.get"] = new RecommendationResponse { Success = true, Recommendations = GenerateMockProducts(5) };

            // Mock response for timeout test
            _mockResponses["test.timeout"] = new ProductResponse { Success = true };
        }

        /// <summary>
        /// Generates mock products for testing
        /// </summary>
        private List<ProductMessage> GenerateMockProducts(int count)
        {
            var products = new List<ProductMessage>();
            for (int i = 0; i < count; i++)
            {
                products.Add(new ProductMessage
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Name = $"Product {i + 1}",
                    Description = $"Description for product {i + 1}",
                    Price = 10.99m + i,
                    QuantityInStock = 100 - i
                });
            }
            return products;
        }

        /// <summary>
        /// Registers handlers for all subjects to avoid NoRespondersException
        /// </summary>
        private void RegisterHandlersForAllSubjects()
        {
            // Register handlers for all subjects to avoid NoRespondersException
            RegisterHandler("cart.additem", async (json) =>
            {
                var message = JsonSerializer.Deserialize<CartMessage>(json);
                var response = new CartResponse { Success = true, SessionId = message?.SessionId, Items = new List<CartItem>() };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("cart.updateitem", async (json) =>
            {
                var message = JsonSerializer.Deserialize<CartMessage>(json);
                var response = new CartResponse { Success = true, SessionId = message?.SessionId, Items = new List<CartItem>() };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("cart.removeitem", async (json) =>
            {
                var message = JsonSerializer.Deserialize<CartMessage>(json);
                var response = new CartResponse { Success = true, SessionId = message?.SessionId, Items = new List<CartItem>() };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("cart.get", async (json) =>
            {
                var message = JsonSerializer.Deserialize<CartMessage>(json);
                var response = new CartResponse { Success = true, SessionId = message?.SessionId, Items = new List<CartItem>() };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("cart.clear", async (json) =>
            {
                var message = JsonSerializer.Deserialize<CartMessage>(json);
                var response = new CartResponse { Success = true, SessionId = message?.SessionId, Items = new List<CartItem>() };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("products.get", async (json) =>
            {
                var message = JsonSerializer.Deserialize<ProductMessage>(json);
                var response = new ProductResponse
                {
                    Success = true,
                    Product = new ProductMessage { ProductId = message?.ProductId ?? Guid.NewGuid().ToString(), Name = "Test Product", Price = 19.99m }
                };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("products.create", async (json) =>
            {
                var message = JsonSerializer.Deserialize<ProductMessage>(json);
                var response = new ProductResponse
                {
                    Success = true,
                    Product = new ProductMessage { ProductId = Guid.NewGuid().ToString(), Name = message?.Name ?? "New Product", Price = message?.Price ?? 19.99m }
                };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("products.update", async (json) =>
            {
                var message = JsonSerializer.Deserialize<ProductMessage>(json);
                var response = new ProductResponse { Success = true };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("products.inventory.update", async (json) =>
            {
                var message = JsonSerializer.Deserialize<ProductMessage>(json);
                var response = new ProductResponse
                {
                    Success = true,
                    Product = new ProductMessage { ProductId = message?.ProductId ?? Guid.NewGuid().ToString(), Name = "Updated Product", QuantityInStock = message?.QuantityInStock ?? 100 }
                };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("products.getall", async (json) =>
            {
                var message = JsonSerializer.Deserialize<ProductMessage>(json);
                var pageNumber = Math.Max(1, message?.PageNumber ?? 1);
                var pageSize = Math.Max(1, message?.PageSize ?? 10);
                var products = GenerateMockProducts(25);
                var pagedProducts = products.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                var response = new ProductListResponse
                {
                    Success = true,
                    Products = pagedProducts,
                    TotalCount = products.Count,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(products.Count / (double)pageSize),
                    HasNextPage = pageNumber < (int)Math.Ceiling(products.Count / (double)pageSize),
                    HasPreviousPage = pageNumber > 1
                };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("recommendations.get", async (json) =>
            {
                var message = JsonSerializer.Deserialize<RecommendationMessage>(json);
                var response = new RecommendationResponse { Success = true, SessionId = message?.SessionId, Recommendations = GenerateMockProducts(5) };
                return JsonSerializer.Serialize(response);
            });

            RegisterHandler("test.timeout", async (json) =>
            {
                // This will be handled specially in RequestAsync
                return "{}";
            });
        }

        /// <summary>
        /// Registers a handler for a subject
        /// </summary>
        public async Task RegisterHandler(string subject, Func<string, Task<string>> handler)
        {
            _handlers[subject] = handler;
            Console.WriteLine($"TestableNatsService: Registered handler for subject: {subject}");
        }

        /// <summary>
        /// Sends a request to a subject with test-specific behavior
        /// </summary>
        public new async Task<TResponse> RequestAsync<TRequest, TResponse>(string subject, TRequest message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            // If we have a handler for this subject, use it
            if (_handlers.TryGetValue(subject, out var handler))
            {
                var json = JsonSerializer.Serialize(message);
                var responseJson = await handler(json);
                var subjectMockResponse = JsonSerializer.Deserialize<TResponse>(responseJson);

                // Handle special cases for testing
                if (subject == "products.get" && message is ProductMessage productGetMessage)
                {
                    if (string.IsNullOrEmpty(productGetMessage.ProductId))
                    {
                        return (TResponse)(object)new ProductResponse
                        {
                            Success = false,
                            Error = "ProductId is required"
                        };
                    }
                }

                if (subject == "products.create" && message is ProductMessage productCreateMessage)
                {
                    if (productCreateMessage.Price < 0)
                    {
                        return (TResponse)(object)new ProductResponse
                        {
                            Success = false,
                            Error = "Price cannot be negative"
                        };
                    }
                }

                return (TResponse)subjectMockResponse;
            }

            // If we're testing timeouts, simulate a timeout
            if (subject == "test.timeout")
            {
                Console.WriteLine($"TestableNatsService: Simulating timeout for subject: {subject}");
                return await HandleTimeoutTestCase<TResponse>(timeout);
            }

            // Handle product and cart message validation
            if (message is ProductMessage productMessage)
            {
                // Special handling for product messages with missing ProductId
                if (string.IsNullOrEmpty(productMessage.ProductId) && subject == "products.get")
                {
                    Console.WriteLine($"TestableNatsService: Product message missing ProductId for subject: {subject}");
                    return (TResponse)(object)new ProductResponse
                    {
                        Success = false,
                        Error = "ProductId is required"
                    };
                }

                // Special handling for product messages with negative price
                if (productMessage.Price < 0 && subject == "products.create")
                {
                    Console.WriteLine($"TestableNatsService: Product message has negative price for subject: {subject}");
                    return (TResponse)(object)new ProductResponse
                    {
                        Success = false,
                        Error = "Price cannot be negative"
                    };
                }

                var result = HandleProductMessage<TResponse>(subject, productMessage);
                if (result != null)
                    return result;
            }
            else if (message is CartMessage cartMessage)
            {
                // Special handling for cart messages with missing session ID
                if (string.IsNullOrEmpty(cartMessage.SessionId) && subject.StartsWith("cart."))
                {
                    Console.WriteLine($"TestableNatsService: Cart message missing SessionId for subject: {subject}");
                    return (TResponse)(object)new CartResponse
                    {
                        Success = false,
                        Error = "SessionId is required"
                    };
                }

                // Special handling for cart messages with negative quantity
                if (cartMessage.Quantity < 0 && (subject == "cart.additem" || subject == "cart.updateitem"))
                {
                    Console.WriteLine($"TestableNatsService: Cart message has negative quantity for subject: {subject}");
                    return (TResponse)(object)new CartResponse
                    {
                        Success = false,
                        Error = "Quantity cannot be negative"
                    };
                }

                var result = HandleCartMessage<TResponse>(subject, cartMessage);
                if (result != null)
                    return result;
            }

            // If we have a mock response for this subject, use it
            if (_mockResponses.TryGetValue(subject, out var mockResponse))
            {
                Console.WriteLine($"TestableNatsService: Using mock response for subject: {subject}");

                // For products.get, customize the response based on the request
                if (subject == "products.get" && message is ProductMessage prodMessage)
                {
                    var response = (ProductResponse)mockResponse;
                    response.Product!.ProductId = prodMessage.ProductId;
                    return (TResponse)(object)response;
                }

                // For products.create, customize the response based on the request
                if (subject == "products.create" && message is ProductMessage createMessage)
                {
                    var response = (ProductResponse)mockResponse;
                    response.Product!.ProductId = createMessage.ProductId ?? Guid.NewGuid().ToString();
                    response.Product.Name = createMessage.Name ?? "New Product";
                    response.Product.Price = createMessage.Price;
                    return (TResponse)(object)response;
                }

                // For cart operations, customize the response based on the request
                if (subject.StartsWith("cart.") && message is CartMessage cartMessage)
                {
                    var response = (CartResponse)mockResponse;
                    response.SessionId = cartMessage.SessionId;
                    return (TResponse)(object)response;
                }

                return (TResponse)mockResponse;
            }

            Console.WriteLine($"TestableNatsService: No handler or mock response for subject: {subject}, creating default response");

            // Create default responses for different message types using a cleaner pattern
            return (TResponse)CreateDefaultResponse<TResponse>();
        }

        /// <summary>
        /// Creates a default response object based on the requested type
        /// </summary>
        private object CreateDefaultResponse<T>()
        {
            return Type.GetTypeCode(typeof(T)) switch
            {
                _ when typeof(T) == typeof(ProductResponse) =>
                    new ProductResponse
                    {
                        Success = true,
                        Product = new ProductMessage
                        {
                            ProductId = Guid.NewGuid().ToString(),
                            Name = "Default Product",
                            Price = 9.99m
                        }
                    },

                _ when typeof(T) == typeof(CartResponse) =>
                    new CartResponse
                    {
                        Success = true,
                        SessionId = "default-session",
                        Items = new List<CartItem>()
                    },

                _ when typeof(T) == typeof(ProductListResponse) =>
                    new ProductListResponse
                    {
                        Success = true,
                        Products = new List<ProductMessage>(),
                        TotalCount = 0
                    },

                _ when typeof(T) == typeof(RecommendationResponse) =>
                    new RecommendationResponse
                    {
                        Success = true,
                        Recommendations = new List<ProductMessage>()
                    },

                // If we can't create a default response, log and throw an exception
                _ => ThrowNoResponder<T>()
            };
        }

        /// <summary>
        /// Helper method for logging and throwing a NatsNoRespondersException.
        /// </summary>
        private T ThrowNoResponder<T>()
        {
            Console.WriteLine($"Unable to create default response for type {typeof(T).Name}");
            throw new NATS.Client.Core.NatsNoRespondersException();
        }

        /// <summary>
        /// Handles the timeout test case
        /// </summary>
        private async Task<TResponse> HandleTimeoutTestCase<TResponse>(TimeSpan? timeout)
        {
            Console.WriteLine("TestableNatsService: Simulating timeout");
            // For test purposes, we'll just throw the exception directly
            // This ensures the test behaves consistently
            throw new NATS.Client.Core.NatsNoRespondersException();
        }

        /// <summary>
        /// Handles product message validation
        /// </summary>
        private TResponse? HandleProductMessage<TResponse>(string subject, ProductMessage message)
        {
            // For products.get, handle special cases
            if (subject == "products.get")
            {
                // Handle missing ProductId for malformed message test
                if (string.IsNullOrEmpty(message.ProductId))
                {
                    return (TResponse)(object)new ProductResponse
                    {
                        Success = false,
                        Error = "ProductId is required"
                    };
                }

                // For non-existent products, return error
                if (message.ProductId == "nonexistent" ||
                    message.ProductId.StartsWith("nonexistent") ||
                    message.ProductId == Guid.Empty.ToString())
                {
                    return (TResponse)(object)new ProductResponse
                    {
                        Success = false,
                        Error = "Product not found"
                    };
                }
            }

            // For products.create, validate the data
            if (subject == "products.create")
            {
                // Check for invalid price
                if (message.Price < 0)
                {
                    return (TResponse)(object)new ProductResponse
                    {
                        Success = false,
                        Error = "Price cannot be negative"
                    };
                }
            }

            return default;
        }

        /// <summary>
        /// Handles cart message validation
        /// </summary>
        private TResponse? HandleCartMessage<TResponse>(string subject, CartMessage message)
        {
            // For cart operations, validate the data
            if (subject.StartsWith("cart."))
            {
                // Handle missing SessionId for malformed message test
                if (string.IsNullOrEmpty(message.SessionId))
                {
                    return (TResponse)(object)new CartResponse
                    {
                        Success = false,
                        Error = "SessionId is required"
                    };
                }

                // For cart.additem and cart.updateitem, check quantity
                if ((subject == "cart.additem" || subject == "cart.updateitem") && message.Quantity < 0)
                {
                    return (TResponse)(object)new CartResponse
                    {
                        Success = false,
                        Error = "Quantity cannot be negative"
                    };
                }

                // For cart.updateitem, handle non-existent items
                if (subject == "cart.updateitem" &&
                    (message.ProductId?.StartsWith("nonexistent") == true ||
                     string.IsNullOrEmpty(message.ProductId)))
                {
                    return (TResponse)(object)new CartResponse
                    {
                        Success = false,
                        Error = "Product not found in cart"
                    };
                }
            }

            return default;
        }

        /// <summary>
        /// Disposes the NATS service
        /// </summary>
        public new async ValueTask DisposeAsync()
        {
            // Call base dispose
            await base.DisposeAsync();
        }
    }
}
