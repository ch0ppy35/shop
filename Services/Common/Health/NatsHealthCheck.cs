using Common.Messaging;
using Microsoft.Extensions.Logging;

namespace Common.Health;

/// <summary>
/// Health check provider for NATS
/// </summary>
public class NatsHealthCheck : IHealthCheck
{
    private readonly ILogger<NatsHealthCheck> _logger;
    private readonly NatsService _natsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsHealthCheck"/> class.
    /// </summary>
    public NatsHealthCheck(ILogger<NatsHealthCheck> logger, NatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    /// <summary>
    /// Gets the name of the health check
    /// </summary>
    public string Name => "NATS";

    /// <summary>
    /// Checks if NATS is ready
    /// </summary>
    public bool IsReady()
    {
        var isReady = _natsService.IsConnected;
        _logger.LogDebug("NATS readiness check, result: {IsReady}", isReady);
        return isReady;
    }
}
