using Common;
using Common.Database;
using Common.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Products.Migrations;
using Products.Repositories;
using Products.Services;

// Get connection string from environment variables
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                      "Host=localhost;Database=products;Username=postgres;Password=postgres";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Add common services
        services.AddCommonServices();

        // Add migration services
        services.AddMigrationServices(connectionString, typeof(InitialMigration).Assembly);

        // Add repositories
        services.AddSingleton<ProductRepository>();

        // Add product service
        services.AddSingleton<ProductService>();

        // Add hosted service
        services.AddHostedService<ProductConsumerService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Products service starting");

// Initialize database connection
var dbService = host.Services.GetRequiredService<DatabaseService>();
logger.LogInformation("Initializing database connection");

try
{
    await dbService.InitializeDatabaseWithRetryAsync();
    logger.LogInformation("Database connection initialized successfully");

    // Run migrations
    logger.LogInformation("Running database migrations");
    var migrationService = host.Services.GetRequiredService<MigrationService>();
    migrationService.RunMigrations();
    logger.LogInformation("Database migrations completed successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Database initialization or migration failed");
    // Exit if database initialization fails
    Environment.Exit(1);
}

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
