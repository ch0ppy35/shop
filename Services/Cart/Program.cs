using Cart.Health;
using Cart.Services;
using Common;
using Common.Health;
using Common.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.AddCommonServices();
builder.Services.AddNatsHealthCheck();
builder.Services.AddRedisHealthCheck();
builder.Services.AddSingleton<RedisService>();
builder.Services.AddSingleton<CartService>();
builder.Services.AddHostedService<CartConsumerService>();
builder.Services.AddHostedService<CartInitializationService>();

// Build and run the host
var host = builder.Build();
await host.RunAsync();

/// <summary>
/// Worker service for initializing connections and health checks
/// </summary>
public class CartInitializationService : BackgroundService
{
    private readonly ILogger<CartInitializationService> _logger;
    private readonly HealthService _healthService;
    private readonly NatsHealthCheck _natsHealthCheck;
    private readonly RedisHealthCheck _redisHealthCheck;
    private readonly INatsService _natsService;
    private readonly RedisService _redisService;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartInitializationService"/> class.
    /// </summary>
    public CartInitializationService(
        ILogger<CartInitializationService> logger,
        HealthService healthService,
        NatsHealthCheck natsHealthCheck,
        RedisHealthCheck redisHealthCheck,
        INatsService natsService,
        RedisService redisService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _healthService = healthService;
        _natsHealthCheck = natsHealthCheck;
        _redisHealthCheck = redisHealthCheck;
        _natsService = natsService;
        _redisService = redisService;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Cart service initialization");

        // Configure health checks
        _healthService.RegisterHealthCheck(_natsHealthCheck);
        _healthService.RegisterHealthCheck(_redisHealthCheck);

        // Start health endpoint first so it's available even when dependencies aren't ready
        var healthEndpoint = new HealthEndpoint(_serviceProvider);
        await healthEndpoint.StartAsync();

        // Step 1: Connect to Redis first
        _logger.LogInformation("Attempting to connect to Redis server with retry mechanism");

        try
        {
            // Use infinite retries (-1) to keep trying to connect
            await _redisService.ConnectWithRetryAsync(-1);
            _logger.LogInformation("Successfully connected to Redis server");

            // Step 2: Only after Redis is ready, connect to NATS
            _logger.LogInformation("Redis is ready, now connecting to NATS server");
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
            _logger.LogError(ex, "Redis connection retry task failed");
            // Don't exit the application, let the health check report the failure
        }

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
