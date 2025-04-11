using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Common.Database;

/// <summary>
/// Service for database operations
/// </summary>
public class DatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseService"/> class.
    /// </summary>
    public DatabaseService(ILogger<DatabaseService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get database connection string from environment variables or configuration
        _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                           configuration.GetConnectionString("DefaultConnection") ??
                           "Host=localhost;Database=postgres;Username=postgres;Password=postgres";

        _logger.LogInformation("Database configuration: ConnectionString={ConnectionString}",
            _connectionString.Replace("Password=", "Password=***"));
    }

    /// <summary>
    /// Creates a new database connection
    /// </summary>
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    /// <summary>
    /// Tests the database connection
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = CreateConnection() as NpgsqlConnection;
            if (connection != null)
            {
                await connection.OpenAsync();
                _logger.LogInformation("Database connection test successful");
                return true;
            }
            _logger.LogError("Failed to create NpgsqlConnection");
            return false;
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
}
