using Common;
using Common.Health;
using Common.Messaging;
using Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add common services
builder.Services.AddCommonServices();

// Add health checks
builder.Services.AddNatsHealthCheck();

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

// Enable CORS so the frontend can access the API
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
});

// Add session middleware to handle session IDs
app.UseMiddleware<SessionMiddleware>();

// Add request logging middleware to log User-Agent
app.UseMiddleware<RequestLoggingMiddleware>();

// app.UseHttpsRedirection();

app.UseAuthorization();

// Map controllers
app.MapControllers();

// Configure health checks
var healthService = app.Services.GetRequiredService<HealthService>();
var natsHealthCheck = app.Services.GetRequiredService<NatsHealthCheck>();
healthService.RegisterHealthCheck(natsHealthCheck);

// Map health check endpoints
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

// Configure the web server to listen on 0.0.0.0:8080
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:8080";
logger.LogInformation("Gateway service listening on {Urls}", urls);

await app.RunAsync();
