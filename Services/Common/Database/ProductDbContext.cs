using Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Common.Database;

/// <summary>
/// Entity Framework Core DbContext for the Products database
/// </summary>
public class ProductDbContext : DbContext
{
    private readonly ILogger<ProductDbContext> _logger;
    private readonly string _connectionString;

    /// <summary>
    /// Gets or sets the Products DbSet
    /// </summary>
    public DbSet<ProductEntity> Products { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductDbContext"/> class.
    /// </summary>
    public ProductDbContext(
        DbContextOptions<ProductDbContext> options,
        ILogger<ProductDbContext> logger,
        IConfiguration configuration) : base(options)
    {
        _logger = logger;

        // Get database connection string from environment variables or configuration
        _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
                           configuration.GetConnectionString("DefaultConnection") ??
                           "Host=localhost;Database=products;Username=postgres;Password=postgres";

        _logger.LogInformation("ProductDbContext initialized with connection string: {ConnectionString}",
            _connectionString.Replace("Password=", "Password=***"));
    }

    /// <summary>
    /// Configures the model for the Products database
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ProductEntity
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityColumn();

            entity.Property(e => e.ProductId)
                .HasColumnName("product_id")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            entity.Property(e => e.Price)
                .HasColumnName("price")
                .HasPrecision(10, 2);

            entity.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .HasDefaultValue(0);

            entity.Property(e => e.Sku)
                .HasColumnName("sku")
                .HasMaxLength(50);

            entity.Property(e => e.Location)
                .HasColumnName("location")
                .HasMaxLength(100);

            entity.Property(e => e.QuantityInStock)
                .HasColumnName("quantity_in_stock")
                .HasDefaultValue(0);

            entity.Property(e => e.ReorderThreshold)
                .HasColumnName("reorder_threshold")
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Add unique constraint on ProductId
            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("ix_products_product_id")
                .IsUnique();

            // Add unique constraint on Sku
            entity.HasIndex(e => e.Sku)
                .HasDatabaseName("ix_products_sku")
                .IsUnique();
        });
    }

    /// <summary>
    /// Configures the database connection
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(_connectionString, options =>
            {
                // Specify the assembly where migrations are located
                options.MigrationsAssembly("Products");
            });
        }
    }
}
