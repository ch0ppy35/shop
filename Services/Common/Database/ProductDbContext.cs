using Common.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Common.Database;

/// <summary>
/// Entity Framework Core DbContext for the Products database
/// </summary>
public class ProductDbContext : DbContext, IProductDbContext
{
    /// <summary>
    /// Gets or sets the Products DbSet
    /// </summary>
    public DbSet<ProductEntity> Products { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductDbContext"/> class.
    /// </summary>
    /// <remarks>
    /// Takes only options so the context can be pooled with AddDbContextPool.
    /// </remarks>
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the model for the Products database
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("ix_products_product_id")
                .IsUnique();

            entity.HasIndex(e => e.Sku)
                .HasDatabaseName("ix_products_sku")
                .IsUnique();
        });
    }
}
