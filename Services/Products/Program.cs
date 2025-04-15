using Common;
using Common.Database;
using Common.Health;
using Common.Messaging;
using Products.Health;
using Products.Repositories;
using Products.Services;

// Get connection string from environment variables
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                      "Host=localhost;Database=products;Username=postgres;Password=postgres";

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.AddCommonServices();
builder.Services.AddNatsHealthCheck();
builder.Services.AddPostgresHealthCheck();
builder.Services.AddDatabaseServices(connectionString);
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ProductSeeder>();
builder.Services.AddHostedService<ProductConsumerService>();
builder.Services.AddHostedService<ProductInitializationService>();

// Build and run the host
var host = builder.Build();
await host.RunAsync();

/// <summary>
/// Worker service for initializing database, connections and health checks
/// </summary>
public class ProductInitializationService : BackgroundService
{
    private readonly ILogger<ProductInitializationService> _logger;
    private readonly HealthService _healthService;
    private readonly NatsHealthCheck _natsHealthCheck;
    private readonly PostgresHealthCheck _postgresHealthCheck;
    private readonly INatsService _natsService;
    private readonly IDatabaseService _dbService;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductInitializationService"/> class.
    /// </summary>
    public ProductInitializationService(
        ILogger<ProductInitializationService> logger,
        HealthService healthService,
        NatsHealthCheck natsHealthCheck,
        PostgresHealthCheck postgresHealthCheck,
        INatsService natsService,
        IDatabaseService dbService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _healthService = healthService;
        _natsHealthCheck = natsHealthCheck;
        _postgresHealthCheck = postgresHealthCheck;
        _natsService = natsService;
        _dbService = dbService;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Products service initialization starting");

        // Configure health checks
        _healthService.RegisterHealthCheck(_natsHealthCheck);
        _healthService.RegisterHealthCheck(_postgresHealthCheck);

        // Start health endpoint before attempting to connect to dependencies
        // so it can report readiness status even when connections are being established
        var healthEndpoint = new HealthEndpoint(_serviceProvider);
        _logger.LogInformation("Starting health endpoint");
        await healthEndpoint.StartAsync();
        _logger.LogInformation("Health endpoint started successfully");

        // Step 1: Initialize database connection
        _logger.LogInformation("Initializing database connection");

        try
        {
            await _dbService.InitializeDatabaseWithRetryAsync();
            _logger.LogInformation("Database connection initialized successfully");

            // Run migrations
            _logger.LogInformation("Running database migrations");
            await _dbService.MigrateAsync();
            _logger.LogInformation("Database migrations completed successfully");

            // Seed database
            _logger.LogInformation("Seeding database");
            using var scope = _serviceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<ProductSeeder>();
            await seeder.SeedAsync();
            _logger.LogInformation("Database seeding completed");

            // Step 2: Only after database is ready, connect to NATS
            _logger.LogInformation("Database is ready, now connecting to NATS server");
            try
            {
                // Use infinite retries (-1) to keep trying to connect
                await _natsService.ConnectWithRetryAsync(-1);
                _logger.LogInformation("Successfully connected to NATS server");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NATS connection retry task failed");
                // Don't exit the application, let the health check report the failure
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization or migration failed");
            // Don't exit the application, let the health check report the failure
        }

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
