using Common.Health;
using Common.Logging;
using Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common;

/// <summary>
/// Extension methods for registering common services
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds common services to the service collection
    /// </summary>
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        // Add JSON logger
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddJsonLogger(config =>
            {
                config.MinimumLogLevel = LogLevel.Information;
            });
        });

        // Add NATS service
        services.AddSingleton<NatsService>();

        // Add health service
        services.AddSingleton<HealthService>();

        return services;
    }
}
