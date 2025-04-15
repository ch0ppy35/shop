using Cart.Services;
using Common.Health;

namespace Cart.Health;

/// <summary>
/// Health check provider for Redis
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly ILogger<RedisHealthCheck> _logger;
    private readonly RedisService _redisService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisHealthCheck"/> class.
    /// </summary>
    public RedisHealthCheck(ILogger<RedisHealthCheck> logger, RedisService redisService)
    {
        _logger = logger;
        _redisService = redisService;
    }

    /// <summary>
    /// Gets the name of the health check
    /// </summary>
    public string Name => "Redis";

    /// <summary>
    /// Checks if Redis is ready
    /// </summary>
    public bool IsReady()
    {
        var isReady = _redisService.IsConnected;
        _logger.LogDebug("Redis readiness check, result: {IsReady}", isReady);
        return isReady;
    }
}
