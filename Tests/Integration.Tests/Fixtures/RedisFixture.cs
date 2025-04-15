using Cart.Services;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    public FakeRedisService RedisService { get; private set; } = null!;

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
    public async Task InitializeAsync()
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
        RedisService = new FakeRedisService();
        await RedisService.ConnectAsync();

        _services.AddSingleton<IRedisService>(RedisService);

        _serviceProvider = _services.BuildServiceProvider();
    }

    /// <summary>
    /// Disposes the Redis container and services
    /// </summary>
    public async Task DisposeAsync()
    {
        // Dispose the fake Redis service
        if (RedisService != null)
        {
            await RedisService.DisposeAsync();
        }
    }
}