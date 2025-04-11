using FluentMigrator;

namespace Products.Migrations;

/// <summary>
/// Initial migration for Products database
/// </summary>
[Migration(20240601001, "Initial Products Schema")]
public class InitialMigration : Migration
{
    /// <summary>
    /// Up migration - creates the products table
    /// </summary>
    public override void Up()
    {
        Create.Table("products")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("product_id").AsString(50).NotNullable().Unique()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("price").AsDecimal(10, 2).NotNullable()
            .WithColumn("quantity").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        // Add indexes
        Create.Index("ix_products_product_id")
            .OnTable("products")
            .OnColumn("product_id");
    }

    /// <summary>
    /// Down migration - drops the products table
    /// </summary>
    public override void Down()
    {
        Delete.Table("products");
    }
}
