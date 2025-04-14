using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Cart.Services;

/// <summary>
/// Service for Redis operations
/// </summary>
public class RedisService : IAsyncDisposable
{
    private readonly ILogger<RedisService> _logger;
    private readonly string _redisConnectionString;
    private ConnectionMultiplexer? _redis;
    private IDatabase? _database;
    private bool _isConnected;
    private readonly TimeSpan _cartTtl = TimeSpan.FromMinutes(15); // 15-minute TTL for carts

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisService"/> class.
    /// </summary>
    public RedisService(ILogger<RedisService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get Redis configuration from environment variables or configuration
        _redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ??
                               configuration.GetValue<string>("Redis:ConnectionString") ??
                               "localhost:6379";

        _logger.LogInformation("Redis configuration: ConnectionString={ConnectionString}", _redisConnectionString);
    }

    /// <summary>
    /// Gets a value indicating whether the service is connected to Redis
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Gets the cart TTL
    /// </summary>
    public TimeSpan CartTtl => _cartTtl;

    /// <summary>
    /// Connects to the Redis server
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await ConnectWithRetryAsync(1, cancellationToken);
    }

    /// <summary>
    /// Connects to the Redis server with retry mechanism
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries, use -1 for infinite retries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ConnectWithRetryAsync(int maxRetries = -1, CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        const int retryDelaySeconds = 10;

        while (!_isConnected && (maxRetries == -1 || retryCount <= maxRetries))
        {
            try
            {
                if (retryCount > 0)
                {
                    _logger.LogInformation("Retry {RetryCount}: Connecting to Redis server at {ConnectionString}",
                        retryCount, _redisConnectionString);
                }
                else
                {
                    _logger.LogInformation("Connecting to Redis server at {ConnectionString}", _redisConnectionString);
                }

                // Dispose previous connection if it exists
                if (_redis != null)
                {
                    await _redis.DisposeAsync();
                }

                // Connect to Redis
                _redis = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString);
                _database = _redis.GetDatabase();

                _isConnected = true;
                _logger.LogInformation("Successfully connected to Redis server");
                return;
            }
            catch (Exception ex)
            {
                _isConnected = false;

                if (maxRetries == -1 || retryCount < maxRetries)
                {
                    _logger.LogWarning(ex, "Failed to connect to Redis server: {Message}. Retrying in {RetryDelay} seconds...",
                        ex.Message, retryDelaySeconds);

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Redis connection retry cancelled");
                        throw;
                    }
                }
                else
                {
                    _logger.LogError(ex, "Failed to connect to Redis server after {RetryCount} retries: {Message}",
                        retryCount, ex.Message);
                    throw;
                }
            }

            retryCount++;
        }
    }

    /// <summary>
    /// Gets a value from Redis
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_database == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected to Redis server");
        }

        try
        {
            _logger.LogDebug("Getting value for key: {Key}", key);
            var value = await _database.StringGetAsync(key);

            if (value.IsNull)
            {
                _logger.LogDebug("No value found for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Value found for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value for key {Key}: {Message}", key, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Sets a value in Redis with TTL
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (_database == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected to Redis server");
        }

        try
        {
            _logger.LogDebug("Setting value for key: {Key}", key);
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, expiry ?? _cartTtl);
            _logger.LogDebug("Value set for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key {Key}: {Message}", key, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Removes a value from Redis
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_database == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected to Redis server");
        }

        try
        {
            _logger.LogDebug("Removing value for key: {Key}", key);
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Value removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value for key {Key}: {Message}", key, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Disposes the Redis connection
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_redis != null)
        {
            await _redis.DisposeAsync();
            _redis = null;
            _database = null;
            _isConnected = false;
            _logger.LogInformation("Redis connection disposed");
        }

        // Call GC.SuppressFinalize to prevent derived types from needing to re-implement IDisposable
        GC.SuppressFinalize(this);
    }
}
