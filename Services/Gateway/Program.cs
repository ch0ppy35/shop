using Common;
using Common.Health;
using Common.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add common services
builder.Services.AddCommonServices();

// Add controllers
builder.Services.AddControllers();

// Configure environment variables
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

// Map controllers
app.MapControllers();

// Health check endpoints
app.MapGet("/healthz", (HealthService healthService) =>
{
    return healthService.IsHealthy() ? Results.Ok() : Results.StatusCode(500);
});

app.MapGet("/readinessz", (HealthService healthService) =>
{
    return healthService.IsReady() ? Results.Ok() : Results.StatusCode(503);
});

// Connect to NATS with retry
var natsService = app.Services.GetRequiredService<NatsService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Start a background task to connect to NATS with infinite retries
_ = Task.Run(async () =>
{
    try
    {
        // Use infinite retries (-1) to keep trying to connect
        await natsService.ConnectWithRetryAsync(-1);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "NATS connection retry task failed");
    }
});

logger.LogInformation("Gateway service started, NATS connection will be established in the background");

await app.RunAsync();
