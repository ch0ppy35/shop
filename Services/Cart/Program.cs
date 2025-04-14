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

// Get services
var natsService = host.Services.GetRequiredService<INatsService>();
var redisService = host.Services.GetRequiredService<RedisService>();

// Start a background task to initialize connections
_ = Task.Run(async () =>
{
    // Step 1: Connect to Redis first
    logger.LogInformation("Attempting to connect to Redis server with retry mechanism");

    try
    {
        // Use infinite retries (-1) to keep trying to connect
        await redisService.ConnectWithRetryAsync(-1);
        logger.LogInformation("Successfully connected to Redis server");

        // Step 2: Only after Redis is ready, connect to NATS
        logger.LogInformation("Redis is ready, now connecting to NATS server");
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
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Redis connection retry task failed");
        // Don't exit the application, let the health check report the failure
    }
});

// Run the host
await host.RunAsync();
