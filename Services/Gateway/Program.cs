using Common;
using Common.Health;
using Common.Messaging;
using Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCommonServices();

builder.Services.AddNatsHealthCheck();

builder.Services.AddControllers();

builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
});

app.UseMiddleware<SessionMiddleware>();

app.UseMiddleware<RequestLoggingMiddleware>();


app.UseAuthorization();

app.MapControllers();

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

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:8080";
logger.LogInformation("Gateway service listening on {Urls}", urls);

await app.RunAsync();
