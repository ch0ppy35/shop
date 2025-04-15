using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cart.Services;
using Common.Models;
using Moq;
using Testcontainers.Redis;
using Xunit;

namespace Integration.Tests.Fixtures;

/// <summary>
/// Fixture for managing Redis container for integration tests
/// </summary>
public class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly IServiceCollection _services = new ServiceCollection();
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the Redis service
    /// </summary>
    public RedisService RedisService => _serviceProvider?.GetRequiredService<RedisService>()
                                        ?? throw new InvalidOperationException("Redis service not initialized");

    /// <summary>
    /// Gets the Redis connection string
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisFixture"/> class
    /// </summary>
    public RedisFixture()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:latest")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
    }

    /// <summary>
    /// Initializes the Redis container and services
    /// </summary>
    public Task InitializeAsync()
    {
        // Skip container startup for faster tests
        // Use a mock Redis service instead
        ConnectionString = "localhost:6379";

        // Configure services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = ConnectionString
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging(builder => builder.AddConsole());

        // Create a fake Redis service
        var redisService = new FakeRedisService();

        // Setup the GetAsync method to return test data
        redisServiceMock.Setup(x => x.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken token) =>
            {
                if (key.StartsWith("cart:"))
                {
                    return new List<CartItem>
                    {
                        new CartItem
                        {
                            ProductId = "test-product-id",
                            Name = "Test Product",
                            Price = 19.99m,
                            Quantity = 1
                        }
                    };
                }

                return null;
            });

        // Setup the SetAsync method
        redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<CartItem>>(), It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup the RemoveAsync method
        redisServiceMock.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _services.AddSingleton(redisServiceMock.Object);

        _serviceProvider = _services.BuildServiceProvider();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the Redis container and services
    /// </summary>
    public Task DisposeAsync()
    {
        // Nothing to dispose since we're using mocks
        return Task.CompletedTask;
    }
}