using FluentMigrator;

namespace Inventory.Migrations;

/// <summary>
/// Initial migration for Inventory database
/// </summary>
[Migration(20240601001, "Initial Inventory Schema")]
public class InitialMigration : Migration
{
    /// <summary>
    /// Up migration - creates the inventory_items table
    /// </summary>
    public override void Up()
    {
        Create.Table("inventory_items")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("inventory_id").AsString(50).NotNullable().Unique()
            .WithColumn("product_id").AsString(50).NotNullable()
            .WithColumn("sku").AsString(50).NotNullable().Unique()
            .WithColumn("location").AsString(100).NotNullable()
            .WithColumn("quantity_in_stock").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("reorder_threshold").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        // Add indexes
        Create.Index("ix_inventory_items_inventory_id")
            .OnTable("inventory_items")
            .OnColumn("inventory_id");

        Create.Index("ix_inventory_items_product_id")
            .OnTable("inventory_items")
            .OnColumn("product_id");

        Create.Index("ix_inventory_items_sku")
            .OnTable("inventory_items")
            .OnColumn("sku");
    }

    /// <summary>
    /// Down migration - drops the inventory_items table
    /// </summary>
    public override void Down()
    {
        Delete.Table("inventory_items");
    }
}
