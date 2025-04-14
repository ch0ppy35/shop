using Cart.Health;
using Cart.Services;
using Common;
using Common.Health;
using Common.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Create the host builder
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
        services.AddRedisHealthCheck();

        // Add Redis service
        services.AddSingleton<RedisService>();

        // Add cart service
        services.AddSingleton<CartService>();

        // Add hosted service
        services.AddHostedService<CartConsumerService>();
    })
    .Build();

// Get logger
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Cart service");

// Configure health checks
var healthService = host.Services.GetRequiredService<HealthService>();
var natsHealthCheck = host.Services.GetRequiredService<NatsHealthCheck>();
var redisHealthCheck = host.Services.GetRequiredService<RedisHealthCheck>();

healthService.RegisterHealthCheck(natsHealthCheck);
healthService.RegisterHealthCheck(redisHealthCheck);

// Start health endpoint first so it's available even when dependencies aren't ready
var healthEndpoint = new HealthEndpoint(host.Services);
await healthEndpoint.StartAsync();

// Connect to NATS with retry
var natsService = host.Services.GetRequiredService<NatsService>();

// For the Cart service, we'll use infinite retries and wait for the connection
// before starting the service
logger.LogInformation("Attempting to connect to NATS server with retry mechanism");

try
{
    // Use infinite retries (-1) to keep trying to connect
    await natsService.ConnectWithRetryAsync(-1);
    logger.LogInformation("Successfully connected to NATS server");
}
catch (Exception ex)
{
    logger.LogError(ex, "NATS connection retry task failed");
    // Exit if NATS connection retry is cancelled or fails
    Environment.Exit(1);
}

// Connect to Redis with retry
var redisService = host.Services.GetRequiredService<RedisService>();
logger.LogInformation("Attempting to connect to Redis server with retry mechanism");

try
{
    // Use infinite retries (-1) to keep trying to connect
    await redisService.ConnectWithRetryAsync(-1);
    logger.LogInformation("Successfully connected to Redis server");
}
catch (Exception ex)
{
    logger.LogError(ex, "Redis connection retry task failed");
    // Exit if Redis connection retry is cancelled or fails
    Environment.Exit(1);
}

// Run the host
await host.RunAsync();
