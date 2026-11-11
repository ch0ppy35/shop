using Common;
using Common.Health;
using Common.Messaging;
using Gateway.Hubs;
using Gateway.Middleware;
using Gateway.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Hybrid;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCommonServices();

builder.Services.AddNatsHealthCheck();

builder.Services.AddControllers();

builder.Configuration.AddEnvironmentVariables();

// Catalog cache: in-process memory, plus Valkey as a shared second level when one is configured
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ??
                            builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "gateway:";
    });
}

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});
builder.Services.AddSingleton<CatalogCache>();

// Live updates: NATS events are forwarded to browsers over SignalR
builder.Services.AddSignalR();
builder.Services.AddHostedService<GatewayEventService>();

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();

app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders(SessionMiddleware.HeaderName)
        // Cache preflights for the 2h browsers allow, instead of Chrome's 5s default
        .SetPreflightMaxAge(TimeSpan.FromHours(2));
});

app.UseMiddleware<SessionMiddleware>();

app.UseMiddleware<RequestLoggingMiddleware>();


app.UseAuthorization();

app.MapControllers();
app.MapHub<ShopHub>(ShopHub.Route);

var healthService = app.Services.GetRequiredService<HealthService>();
var natsHealthCheck = app.Services.GetRequiredService<NatsHealthCheck>();
healthService.RegisterHealthCheck(natsHealthCheck);

app.MapGet("/healthz", () =>
{
    return healthService.IsHealthy()
        ? Results.Ok(new { status = "healthy" })
        : Results.StatusCode(500);
});

app.MapGet("/readinessz", () =>
{
    return healthService.IsReady()
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(503);
});

var natsService = app.Services.GetRequiredService<INatsService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

_ = Task.Run(async () =>
{
    try
    {
        await natsService.ConnectWithRetryAsync(-1);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "NATS connection retry task failed");
    }
});

logger.LogInformation("Gateway service started, NATS connection will be established in the background");
logger.LogInformation("Catalog cache second level: {CacheLevel2}",
    string.IsNullOrEmpty(redisConnectionString) ? "none (memory only)" : "Valkey");

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:8080";
logger.LogInformation("Gateway service listening on {Urls}", urls);

await app.RunAsync();