using Common.Database;
using Common.Health;
using Common.Logging;
using Common.Messaging;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
    /// Adds migration services to the service collection
    /// </summary>
    public static IServiceCollection AddMigrationServices(this IServiceCollection services, string connectionString, Assembly migrationsAssembly)
    {
        // Configure FluentMigrator
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(migrationsAssembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        // Add migration service
        services.AddSingleton<MigrationService>();

        return services;
    }
}
