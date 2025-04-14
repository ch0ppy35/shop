using Common.Database;
using Common.Health;
using Common.Logging;
using Common.Messaging;
using Microsoft.EntityFrameworkCore;
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

        // Add database service
        services.AddSingleton<DatabaseService>();

        return services;
    }

    /// <summary>
    /// Adds Entity Framework Core database services to the service collection
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, string connectionString)
    {
        // Configure Entity Framework Core
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Specify the assembly where migrations are located
                npgsqlOptions.MigrationsAssembly("Products");
            }));

        // Register the DbContext as the interface implementation
        services.AddScoped<IProductDbContext>(provider => provider.GetRequiredService<ProductDbContext>());

        return services;
    }

    /// <summary>
    /// Adds NATS health check to the service collection
    /// </summary>
    public static IServiceCollection AddNatsHealthCheck(this IServiceCollection services)
    {
        services.AddSingleton<NatsHealthCheck>();
        return services;
    }

    /// <summary>
    /// Adds PostgreSQL health check to the service collection
    /// </summary>
    public static IServiceCollection AddPostgresHealthCheck(this IServiceCollection services)
    {
        services.AddSingleton<PostgresHealthCheck>();
        return services;
    }


}
