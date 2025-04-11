using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Common.Database;

/// <summary>
/// Service for running database migrations
/// </summary>
public class MigrationService
{
    private readonly ILogger<MigrationService> _logger;
    private readonly string _connectionString;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationService"/> class.
    /// </summary>
    public MigrationService(ILogger<MigrationService> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Get database connection string from environment variables or configuration
        _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                           configuration.GetConnectionString("DefaultConnection") ??
                           "Host=localhost;Database=postgres;Username=postgres;Password=postgres";

        _logger.LogInformation("Migration service initialized with connection string: {ConnectionString}",
            _connectionString.Replace("Password=", "Password=***"));
    }

    /// <summary>
    /// Runs all pending migrations
    /// </summary>
    public void RunMigrations()
    {
        _logger.LogInformation("Running database migrations");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp();

            _logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running database migrations: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Rolls back the last migration
    /// </summary>
    public void RollbackLastMigration()
    {
        _logger.LogInformation("Rolling back last database migration");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            runner.Rollback(1);

            _logger.LogInformation("Database migration rollback completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back database migration: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Configures FluentMigrator services
    /// </summary>
    public static IServiceCollection ConfigureMigrationServices(IServiceCollection services, string connectionString, Assembly migrationsAssembly)
    {
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(migrationsAssembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }
}
