using Common.Messaging;
using Microsoft.Extensions.Logging;

namespace Common.Health;

/// <summary>
/// Service for checking the health of the application
/// </summary>
public class HealthService
{
    private readonly ILogger<HealthService> _logger;
    private readonly NatsService _natsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthService"/> class.
    /// </summary>
    public HealthService(ILogger<HealthService> logger, NatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    /// <summary>
    /// Checks if the application is healthy
    /// </summary>
    public bool IsHealthy()
    {
        // Basic health check - always returns true
        _logger.LogDebug("Health check requested");
        return true;
    }

    /// <summary>
    /// Checks if the application is ready to serve requests
    /// </summary>
    public bool IsReady()
    {
        // Check if NATS is connected
        var isReady = _natsService.IsConnected;
        _logger.LogDebug("Readiness check requested, result: {IsReady}", isReady);
        return isReady;
    }
}
