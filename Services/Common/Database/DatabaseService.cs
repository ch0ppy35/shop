using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Common.Database;

/// <summary>
/// Service for database operations using Entity Framework Core
/// </summary>
public class DatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseService"/> class.
    /// </summary>
    public DatabaseService(ILogger<DatabaseService> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Get database connection string from environment variables or configuration
        _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                           configuration.GetConnectionString("DefaultConnection") ??
                           "Host=localhost;Database=products;Username=postgres;Password=postgres";

        _logger.LogInformation("Database configuration: ConnectionString={ConnectionString}",
            _connectionString.Replace("Password=", "Password=***"));
    }

    /// <summary>
    /// Creates a new DbContext scope and returns the ProductDbContext
    /// </summary>
    /// <remarks>
    /// This method should be used with caution. It's better to inject the DbContext directly
    /// where possible to let the DI container manage its lifecycle.
    /// </remarks>
    public IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }

    /// <summary>
    /// Tests the database connection
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogInformation("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Initializes the database with retry mechanism
    /// </summary>
    public async Task InitializeDatabaseWithRetryAsync(int maxRetries = 5, int retryDelaySeconds = 5)
    {
        var retryCount = 0;
        var connected = false;

        while (!connected && retryCount < maxRetries)
        {
            try
            {
                if (retryCount > 0)
                {
                    _logger.LogInformation("Retry {RetryCount}: Connecting to database", retryCount);
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }

                connected = await TestConnectionAsync();

                if (connected)
                {
                    _logger.LogInformation("Successfully connected to database");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to database: {Message}", ex.Message);
            }

            retryCount++;
        }

        if (!connected)
        {
            _logger.LogError("Failed to connect to database after {RetryCount} retries", retryCount);
            throw new Exception("Failed to connect to database");
        }
    }

    /// <summary>
    /// Applies any pending migrations
    /// </summary>
    public async Task MigrateAsync()
    {
        try
        {
            _logger.LogInformation("Applying database migrations");
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

            // Ensure the database exists
            await dbContext.Database.EnsureCreatedAsync();

            // Apply migrations
            await dbContext.Database.MigrateAsync();

            _logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying database migrations: {Message}", ex.Message);
            throw;
        }
    }
}
