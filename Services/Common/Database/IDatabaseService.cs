using Microsoft.Extensions.DependencyInjection;

namespace Common.Database;

/// <summary>
/// Interface for database operations using Entity Framework Core
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Creates a new DbContext scope and returns the ProductDbContext
    /// </summary>
    IServiceScope CreateScope();

    /// <summary>
    /// Tests the database connection
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Initializes the database with retry mechanism
    /// </summary>
    Task InitializeDatabaseWithRetryAsync(int maxRetries = 5, int retryDelaySeconds = 5);

    /// <summary>
    /// Applies any pending migrations
    /// </summary>
    Task MigrateAsync();
}