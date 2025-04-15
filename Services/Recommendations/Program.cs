using Common;
using Common.Health;
using Common.Messaging;
using Recommendations.Health;
using Recommendations.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Add common services
        services.AddCommonServices();

        // Add health checks
        services.AddNatsHealthCheck();

        // Add recommendation service
        services.AddScoped<IRecommendationService, RecommendationService>();

        // Add hosted service
        services.AddHostedService<RecommendationConsumerService>();
    })
    .Build();

// Get services
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var natsService = host.Services.GetRequiredService<INatsService>();
var healthService = host.Services.GetRequiredService<HealthService>();
var natsHealthCheck = host.Services.GetRequiredService<NatsHealthCheck>();

// Register health checks
healthService.RegisterHealthCheck(natsHealthCheck);

// Start health endpoint
var healthEndpoint = new HealthEndpoint(host.Services);
logger.LogInformation("Starting health endpoint");
await healthEndpoint.StartAsync();
logger.LogInformation("Health endpoint started successfully");

// Start a background task to connect to NATS with infinite retries
_ = Task.Run(async () =>
{
    try
    {
        // Use infinite retries (-1) to keep trying to connect
        await natsService.ConnectWithRetryAsync(-1);
        logger.LogInformation("Successfully connected to NATS server");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "NATS connection retry task failed");
        // Don't exit the application, let the health check report the failure
    }
});

logger.LogInformation("Recommendation service starting");

// Run the host
await host.RunAsync();
