using Cart.Health;
using Cart.Services;
using Common;
using Common.Health;
using Common.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCommonServices();
builder.Services.AddNatsHealthCheck();
builder.Services.AddRedisHealthCheck();
builder.Services.AddSingleton<IRedisService, RedisService>();
builder.Services.AddSingleton<RedisService>(sp => (RedisService)sp.GetRequiredService<IRedisService>());
builder.Services.AddSingleton<CartService>();
builder.Services.AddHostedService<CartConsumerService>();
builder.Services.AddHostedService<CartInitializationService>();

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

        _healthService.RegisterHealthCheck(_natsHealthCheck);
        _healthService.RegisterHealthCheck(_redisHealthCheck);

        var healthEndpoint = new HealthEndpoint(_serviceProvider);
        await healthEndpoint.StartAsync();

        _logger.LogInformation("Attempting to connect to Redis server with retry mechanism");

        try
        {
            await _redisService.ConnectWithRetryAsync(-1);
            _logger.LogInformation("Successfully connected to Redis server");

            _logger.LogInformation("Redis is ready, now connecting to NATS server");
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
            _logger.LogError(ex, "Redis connection retry task failed");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}