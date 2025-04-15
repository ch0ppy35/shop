using Common;
using Common.Database;
using Common.Health;
using Common.Messaging;
using Products.Health;
using Products.Repositories;
using Products.Services;


var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                      "Host=localhost;Database=products;Username=postgres;Password=postgres";

var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddCommonServices();
builder.Services.AddNatsHealthCheck();
builder.Services.AddPostgresHealthCheck();
builder.Services.AddDatabaseServices(connectionString);
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ProductSeeder>();
builder.Services.AddHostedService<ProductConsumerService>();
builder.Services.AddHostedService<ProductInitializationService>();


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


        _healthService.RegisterHealthCheck(_natsHealthCheck);
        _healthService.RegisterHealthCheck(_postgresHealthCheck);


        var healthEndpoint = new HealthEndpoint(_serviceProvider);
        _logger.LogInformation("Starting health endpoint");
        await healthEndpoint.StartAsync();
        _logger.LogInformation("Health endpoint started successfully");


        _logger.LogInformation("Initializing database connection");

        try
        {
            await _dbService.InitializeDatabaseWithRetryAsync();
            _logger.LogInformation("Database connection initialized successfully");


            _logger.LogInformation("Running database migrations");
            await _dbService.MigrateAsync();
            _logger.LogInformation("Database migrations completed successfully");


            _logger.LogInformation("Seeding database");
            using var scope = _serviceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<ProductSeeder>();
            await seeder.SeedAsync();
            _logger.LogInformation("Database seeding completed");


            _logger.LogInformation("Database is ready, now connecting to NATS server");
            try
            {

                await _natsService.ConnectWithRetryAsync(-1);
                _logger.LogInformation("Successfully connected to NATS server");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NATS connection retry task failed");

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization or migration failed");

        }


        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
