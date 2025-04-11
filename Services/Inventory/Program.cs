using Common;
using Common.Database;
using Common.Messaging;
using Inventory.Migrations;
using Inventory.Repositories;
using Inventory.Services;

// Get connection string from environment variables
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                      "Host=localhost;Database=inventory;Username=postgres;Password=postgres";

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
        services.AddSingleton<InventoryRepository>();

        // Add inventory service
        services.AddSingleton<InventoryService>();

        // Add hosted service
        services.AddHostedService<InventoryConsumerService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Inventory service starting");

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

// Get the NATS service and connect to NATS
var natsService = host.Services.GetRequiredService<NatsService>();

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
