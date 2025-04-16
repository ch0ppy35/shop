using Cart.Services;
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
    public RedisService RedisService { get; private set; } = null!;

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
            .WithImage("valkey/valkey:8.1-alpine")
            .WithPortBinding(6379, true)
            .Build();
    }

    /// <summary>
    /// Initializes the Redis container and services
    /// </summary>
    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting Redis container...");
        // Start the Redis container
        await _redisContainer.StartAsync();
        ConnectionString = _redisContainer.GetConnectionString();
        Console.WriteLine($"Redis container started with connection string: {ConnectionString}");

        // Configure services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = ConnectionString
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging(builder => builder.AddConsole());

        // Create and register the real Redis service
        var logger = _services.BuildServiceProvider().GetRequiredService<ILogger<RedisService>>();
        Console.WriteLine("Creating RedisService...");
        RedisService = new RedisService(logger, configuration);
        Console.WriteLine("Connecting to Redis...");
        try
        {
            await RedisService.ConnectWithRetryAsync(5);
            Console.WriteLine("Successfully connected to Redis");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to Redis: {ex.Message}");
            throw;
        }

        // Register the Redis service
        _services.AddSingleton<IRedisService>(RedisService);

        _serviceProvider = _services.BuildServiceProvider();
    }

    /// <summary>
    /// Disposes the Redis container and services
    /// </summary>
    public async Task DisposeAsync()
    {
        // Dispose the Redis service
        if (RedisService != null)
        {
            await RedisService.DisposeAsync();
        }

        await _redisContainer.DisposeAsync();
    }
}