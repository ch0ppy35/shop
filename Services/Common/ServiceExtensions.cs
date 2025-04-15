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
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddJsonLogger(config =>
            {
                config.MinimumLogLevel = LogLevel.Information;
            });
        });

        services.AddSingleton<INatsService, NatsService>();
        services.AddSingleton<NatsService>(sp => (NatsService)sp.GetRequiredService<INatsService>());

        services.AddSingleton<HealthService>();

        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<DatabaseService>(sp => (DatabaseService)sp.GetRequiredService<IDatabaseService>());

        return services;
    }

    /// <summary>
    /// Adds Entity Framework Core database services to the service collection
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Products");
            }));

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
