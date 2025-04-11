using Common;
using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Inventory.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Add common services
        services.AddCommonServices();

        // Add inventory service
        services.AddSingleton<InventoryService>();

        // Add hosted service
        services.AddHostedService<InventoryConsumerService>();
    })
    .Build();

// Get the NATS service and connect to NATS
var natsService = host.Services.GetRequiredService<NatsService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    // Connect to NATS with infinite retries
    await natsService.ConnectWithRetryAsync(-1);
    logger.LogInformation("Connected to NATS server");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to connect to NATS server");
    throw;
}

// Run the host
await host.RunAsync();
