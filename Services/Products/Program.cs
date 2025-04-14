using Common;
using Common.Database;
using Common.Health;
using Common.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Products.Health;
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

        // Add health checks
        services.AddNatsHealthCheck();
        services.AddPostgresHealthCheck();

        // Add database services
        services.AddDatabaseServices(connectionString);

        // Add repositories
        services.AddScoped<IProductRepository, ProductRepository>();

        // Add product service
        services.AddScoped<IProductService, ProductService>();

        // Add product seeder
        services.AddScoped<ProductSeeder>();

        // Add hosted service
        services.AddHostedService<ProductConsumerService>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Products service starting");

// Configure health checks first
var healthService = host.Services.GetRequiredService<HealthService>();
var natsHealthCheck = host.Services.GetRequiredService<NatsHealthCheck>();
var postgresHealthCheck = host.Services.GetRequiredService<PostgresHealthCheck>();

healthService.RegisterHealthCheck(natsHealthCheck);
healthService.RegisterHealthCheck(postgresHealthCheck);

// Start health endpoint before attempting to connect to dependencies
// so it can report readiness status even when connections are being established
var healthEndpoint = new HealthEndpoint(host.Services);
logger.LogInformation("Starting health endpoint");
await healthEndpoint.StartAsync();
logger.LogInformation("Health endpoint started successfully");

// Initialize database and NATS connections in the background
var dbService = host.Services.GetRequiredService<IDatabaseService>();
var natsService = host.Services.GetRequiredService<INatsService>();

// Start a background task to initialize the database and then connect to NATS
_ = Task.Run(async () =>
{
    // Step 1: Initialize database connection
    logger.LogInformation("Initializing database connection in background");

    try
    {
        await dbService.InitializeDatabaseWithRetryAsync();
        logger.LogInformation("Database connection initialized successfully");

        // Run migrations
        logger.LogInformation("Running database migrations");
        await dbService.MigrateAsync();
        logger.LogInformation("Database migrations completed successfully");

        // Seed database
        logger.LogInformation("Seeding database");
        using var scope = host.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ProductSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database seeding completed");

        // Step 2: Only after database is ready, connect to NATS
        logger.LogInformation("Database is ready, now connecting to NATS server");
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
        logger.LogError(ex, "Database initialization or migration failed");
        // Don't exit the application, let the health check report the failure
    }
});

// Run the host
await host.RunAsync();
