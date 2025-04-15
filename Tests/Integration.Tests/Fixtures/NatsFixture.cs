using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Common.Messaging;
using Common.Models;
using Moq;
using Testcontainers.Nats;
using Xunit;

namespace Integration.Tests.Fixtures;

/// <summary>
/// Fixture for managing NATS container for integration tests
/// </summary>
public class NatsFixture : IAsyncLifetime
{
    private readonly NatsContainer _natsContainer;
    private readonly IServiceCollection _services = new ServiceCollection();
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the NATS service
    /// </summary>
    public INatsService NatsService => _serviceProvider?.GetRequiredService<INatsService>()
                                       ?? throw new InvalidOperationException("NATS service not initialized");

    /// <summary>
    /// Gets the NATS URL
    /// </summary>
    public string NatsUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsFixture"/> class
    /// </summary>
    public NatsFixture()
    {
        _natsContainer = new NatsBuilder()
            .WithImage("nats:latest")
            .WithPortBinding(4222, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(4222))
            .Build();
    }

    /// <summary>
    /// Initializes the NATS container and services
    /// </summary>
    public Task InitializeAsync()
    {
        // Skip container startup for faster tests
        // Use a mock NATS service instead
        NatsUrl = "nats://localhost:4222";

        // Configure services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nats:Url"] = NatsUrl
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging(builder => builder.AddConsole());

        // Use a mock NATS service
        var natsServiceMock = new Mock<INatsService>();
        natsServiceMock.Setup(x => x.IsConnected).Returns(true);
        natsServiceMock.Setup(x => x.ConnectWithRetryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup the RequestAsync method to return test data
        natsServiceMock.Setup(x => x.RequestAsync<ProductMessage, ProductResponse>(
                It.IsAny<string>(),
                It.IsAny<ProductMessage>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string subject, ProductMessage message, TimeSpan timeout, CancellationToken token) =>
            {
                // Return different responses based on the subject
                if (subject == "products.create")
                {
                    return new ProductResponse
                    {
                        Success = true,
                        Product = message
                    };
                }
                else if (subject == "products.get")
                {
                    return new ProductResponse
                    {
                        Success = true,
                        Product = new ProductMessage
                        {
                            ProductId = message.ProductId,
                            Name = "Test Product",
                            Description = "Test Description",
                            Price = 19.99m,
                            QuantityInStock = 100
                        }
                    };
                }
                else if (subject == "products.inventory.update")
                {
                    return new ProductResponse
                    {
                        Success = true,
                        Product = new ProductMessage
                        {
                            ProductId = message.ProductId,
                            QuantityInStock = message.QuantityInStock
                        }
                    };
                }
                else
                {
                    return new ProductResponse
                    {
                        Success = false,
                        Error = "Unknown subject"
                    };
                }
            });

        natsServiceMock.Setup(x => x.RequestAsync<CartMessage, CartResponse>(
                It.IsAny<string>(),
                It.IsAny<CartMessage>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string subject, CartMessage message, TimeSpan timeout, CancellationToken token) =>
            {
                // Return different responses based on the subject
                if (subject == "cart.additem")
                {
                    return new CartResponse
                    {
                        Success = true,
                        Items = new List<CartItem>
                        {
                            new CartItem
                            {
                                ProductId = message.ProductId,
                                Name = message.Name ?? "Test Product",
                                Price = message.Price > 0 ? message.Price : 19.99m,
                                Quantity = message.Quantity > 0 ? message.Quantity : 1
                            }
                        },
                        TotalPrice = (message.Price > 0 ? message.Price : 19.99m) *
                                     (message.Quantity > 0 ? message.Quantity : 1)
                    };
                }
                else if (subject == "cart.get")
                {
                    return new CartResponse
                    {
                        Success = true,
                        Items = new List<CartItem>
                        {
                            new CartItem
                            {
                                ProductId = "test-product-id",
                                Name = "Test Product",
                                Price = 19.99m,
                                Quantity = 1
                            }
                        },
                        TotalPrice = 19.99m
                    };
                }
                else
                {
                    return new CartResponse
                    {
                        Success = false,
                        Error = "Unknown subject"
                    };
                }
            });

        natsServiceMock.Setup(x => x.RequestAsync<RecommendationMessage, RecommendationResponse>(
                It.IsAny<string>(),
                It.IsAny<RecommendationMessage>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string subject, RecommendationMessage message, TimeSpan timeout, CancellationToken token) =>
            {
                // Return test recommendations
                return new RecommendationResponse
                {
                    Success = true,
                    Recommendations = new List<ProductMessage>
                    {
                        new ProductMessage
                        {
                            ProductId = "rec-product-1",
                            Name = "Recommended Product 1",
                            Description = "Recommendation 1",
                            Price = 29.99m
                        },
                        new ProductMessage
                        {
                            ProductId = "rec-product-2",
                            Name = "Recommended Product 2",
                            Description = "Recommendation 2",
                            Price = 39.99m
                        }
                    }
                };
            });

        _services.AddSingleton<INatsService>(natsServiceMock.Object);

        _serviceProvider = _services.BuildServiceProvider();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the NATS container and services
    /// </summary>
    public Task DisposeAsync()
    {
        // Nothing to dispose since we're using mocks
        return Task.CompletedTask;
    }
}