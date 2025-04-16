using Common.Health;
using Common.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Recommendations.Health;

/// <summary>
/// Health check endpoint for the Recommendations service
/// </summary>
public class HealthEndpoint : IDisposable
{
    private readonly ILogger<HealthEndpoint> _logger;
    private readonly HealthService _healthService;
    private readonly WebApplication _app;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthEndpoint"/> class.
    /// </summary>
    public HealthEndpoint(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<HealthEndpoint>>();
        _healthService = serviceProvider.GetRequiredService<HealthService>();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(8081); });

        builder.Logging.ClearProviders();
        builder.Logging.AddJsonLogger(config => { config.MinimumLogLevel = LogLevel.Information; });

        _app = builder.Build();

        _app.MapGet("/healthz", () =>
        {
            return _healthService.IsHealthy()
                ? Results.Ok(new { status = "healthy" })
                : Results.StatusCode(500);
        });

        _app.MapGet("/readinessz", () =>
        {
            return _healthService.IsReady()
                ? Results.Ok(new { status = "ready" })
                : Results.StatusCode(503);
        });

        _logger.LogInformation("Health endpoint initialized on port 8081");
    }

    /// <summary>
    /// Starts the health endpoint
    /// </summary>
    public async Task StartAsync()
    {
        _logger.LogInformation("Starting health endpoint");
        await _app.StartAsync();
        _logger.LogInformation("Health endpoint started");
    }

    /// <summary>
    /// Disposes the health endpoint
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the health endpoint
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Task.Run(async () => await _app.StopAsync()).GetAwaiter().GetResult();
                Task.Run(async () => await _app.DisposeAsync()).GetAwaiter().GetResult();
                _logger.LogInformation("Health endpoint disposed");
            }

            _disposed = true;
        }
    }
}