using Common;
using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Products.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Add common services
        services.AddCommonServices();

        // Add product service
        services.AddSingleton<ProductService>();

        // Add hosted service
        services.AddHostedService<ProductConsumerService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Products service starting");

// Connect to NATS with retry
var natsService = host.Services.GetRequiredService<NatsService>();

// For the Products service, we'll use infinite retries and wait for the connection
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

await host.RunAsync();
