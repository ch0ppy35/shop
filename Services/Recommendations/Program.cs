using Common;
using Common.Health;
using Common.Messaging;
using Recommendations.Health;
using Recommendations.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCommonServices();
builder.Services.AddNatsHealthCheck();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddHostedService<RecommendationConsumerService>();
builder.Services.AddHostedService<RecommendationInitializationService>();

var host = builder.Build();
await host.RunAsync();

/// <summary>
/// Worker service for initializing connections and health checks
/// </summary>
public class RecommendationInitializationService : BackgroundService
{
    private readonly ILogger<RecommendationInitializationService> _logger;
    private readonly HealthService _healthService;
    private readonly NatsHealthCheck _natsHealthCheck;
    private readonly INatsService _natsService;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationInitializationService"/> class.
    /// </summary>
    public RecommendationInitializationService(
        ILogger<RecommendationInitializationService> logger,
        HealthService healthService,
        NatsHealthCheck natsHealthCheck,
        INatsService natsService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _healthService = healthService;
        _natsHealthCheck = natsHealthCheck;
        _natsService = natsService;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recommendation service initialization starting");

        _healthService.RegisterHealthCheck(_natsHealthCheck);

        var healthEndpoint = new HealthEndpoint(_serviceProvider);
        _logger.LogInformation("Starting health endpoint");
        await healthEndpoint.StartAsync();
        _logger.LogInformation("Health endpoint started successfully");

        try
        {
            await _natsService.ConnectWithRetryAsync(-1);
            _logger.LogInformation("Successfully connected to NATS server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NATS connection retry task failed");
        }

        _logger.LogInformation("Recommendation service initialization completed");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}