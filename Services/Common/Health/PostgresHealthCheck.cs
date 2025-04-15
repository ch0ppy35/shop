using Common.Database;
using Microsoft.Extensions.Logging;

namespace Common.Health;

/// <summary>
/// Health check provider for PostgreSQL
/// </summary>
public class PostgresHealthCheck : IHealthCheck
{
    private readonly ILogger<PostgresHealthCheck> _logger;
    private readonly IDatabaseService _databaseService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresHealthCheck"/> class.
    /// </summary>
    public PostgresHealthCheck(ILogger<PostgresHealthCheck> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Gets the name of the health check
    /// </summary>
    public string Name => "PostgreSQL";

    /// <summary>
    /// Checks if PostgreSQL is ready
    /// </summary>
    public bool IsReady()
    {
        try
        {
            var isReady = _databaseService.TestConnectionAsync().GetAwaiter().GetResult();
            _logger.LogDebug("PostgreSQL readiness check, result: {IsReady}", isReady);
            return isReady;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking PostgreSQL readiness");
            return false;
        }
    }
}
