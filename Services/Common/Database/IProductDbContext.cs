using Common.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Common.Database;

/// <summary>
/// Interface for the ProductDbContext
/// </summary>
public interface IProductDbContext
{
    /// <summary>
    /// Gets or sets the Products DbSet
    /// </summary>
    DbSet<ProductEntity> Products { get; set; }

    /// <summary>
    /// Saves changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}