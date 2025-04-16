using Microsoft.Extensions.Logging;

namespace Common.Health;

/// <summary>
/// Service for checking the health of the application
/// </summary>
public class HealthService
{
    private readonly ILogger<HealthService> _logger;
    private readonly List<IHealthCheck> _healthChecks = new();
    private bool _lastReadinessStatus = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthService"/> class.
    /// </summary>
    public HealthService(ILogger<HealthService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a health check provider
    /// </summary>
    public void RegisterHealthCheck(IHealthCheck healthCheck)
    {
        _healthChecks.Add(healthCheck);
        _logger.LogInformation("Registered health check: {HealthCheckName}", healthCheck.Name);
    }

    /// <summary>
    /// Checks if the application is healthy
    /// </summary>
    /// <remarks>
    /// The health check is used by Kubernetes to determine if the application is still running.
    /// It should always return true unless the application is completely unresponsive.
    /// </remarks>
    public bool IsHealthy()
    {
        _logger.LogDebug("Health check requested");
        return true;
    }

    /// <summary>
    /// Checks if the application is ready to serve requests
    /// </summary>
    /// <remarks>
    /// The readiness check is used by Kubernetes to determine if the application is ready to receive traffic.
    /// It should return true only when all required dependencies are available.
    /// </remarks>
    public bool IsReady()
    {
        if (_healthChecks.Count == 0)
        {
            _logger.LogWarning("No health checks registered, readiness check will always return true");
            return true;
        }

        var isReady = true;
        var readinessDetails = new List<string>();

        foreach (var healthCheck in _healthChecks)
        {
            var componentReady = healthCheck.IsReady();
            isReady &= componentReady;
            readinessDetails.Add($"{healthCheck.Name}: {(componentReady ? "Ready" : "Not Ready")}");
        }

        if (isReady != _lastReadinessStatus)
        {
            var logLevel = isReady ? LogLevel.Information : LogLevel.Warning;
            _logger.Log(logLevel, "Readiness status changed to {IsReady}", isReady);
            _lastReadinessStatus = isReady;
        }

        _logger.LogDebug("Readiness check requested, result: {IsReady}, Details: {Details}",
            isReady, string.Join(", ", readinessDetails));

        return isReady;
    }
}