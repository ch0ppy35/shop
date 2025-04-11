using Common.Database;
using Common.Database.Models;
using Common.Models;
using Dapper;

namespace Inventory.Repositories;

/// <summary>
/// Repository for inventory data
/// </summary>
public class InventoryRepository
{
    private readonly ILogger<InventoryRepository> _logger;
    private readonly DatabaseService _databaseService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryRepository"/> class.
    /// </summary>
    public InventoryRepository(ILogger<InventoryRepository> logger, DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Gets all inventory items
    /// </summary>
    public async Task<IEnumerable<InventoryEntity>> GetAllInventoryAsync()
    {
        _logger.LogInformation("Getting all inventory items from database");

        using var connection = _databaseService.CreateConnection();
        var sql = "SELECT * FROM inventory_items";

        return await connection.QueryAsync<InventoryEntity>(sql);
    }

    /// <summary>
    /// Gets an inventory item by ID
    /// </summary>
    public async Task<InventoryEntity?> GetInventoryByIdAsync(string inventoryId)
    {
        _logger.LogInformation("Getting inventory item with ID: {InventoryId} from database", inventoryId);

        using var connection = _databaseService.CreateConnection();
        var sql = "SELECT * FROM inventory_items WHERE inventory_id = @InventoryId";

        return await connection.QueryFirstOrDefaultAsync<InventoryEntity>(sql, new { InventoryId = inventoryId });
    }

    /// <summary>
    /// Creates a new inventory item
    /// </summary>
    public async Task<InventoryEntity> CreateInventoryAsync(InventoryEntity inventory)
    {
        _logger.LogInformation("Creating new inventory item with ID: {InventoryId} in database", inventory.InventoryId);

        using var connection = _databaseService.CreateConnection();
        var sql = @"
            INSERT INTO inventory_items (inventory_id, product_id, sku, location, quantity_in_stock, reorder_threshold, created_at, updated_at)
            VALUES (@InventoryId, @ProductId, @Sku, @Location, @QuantityInStock, @ReorderThreshold, @CreatedAt, @UpdatedAt)
            RETURNING *";

        return await connection.QueryFirstAsync<InventoryEntity>(sql, inventory);
    }

    /// <summary>
    /// Updates an existing inventory item
    /// </summary>
    public async Task<bool> UpdateInventoryAsync(InventoryEntity inventory)
    {
        _logger.LogInformation("Updating inventory item with ID: {InventoryId} in database", inventory.InventoryId);

        using var connection = _databaseService.CreateConnection();
        var sql = @"
            UPDATE inventory_items
            SET product_id = @ProductId,
                sku = @Sku,
                location = @Location,
                quantity_in_stock = @QuantityInStock,
                reorder_threshold = @ReorderThreshold,
                updated_at = @UpdatedAt
            WHERE inventory_id = @InventoryId";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            inventory.InventoryId,
            inventory.ProductId,
            inventory.Sku,
            inventory.Location,
            inventory.QuantityInStock,
            inventory.ReorderThreshold,
            UpdatedAt = DateTime.UtcNow
        });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Deletes an inventory item
    /// </summary>
    public async Task<bool> DeleteInventoryAsync(string inventoryId)
    {
        _logger.LogInformation("Deleting inventory item with ID: {InventoryId} from database", inventoryId);

        using var connection = _databaseService.CreateConnection();
        var sql = "DELETE FROM inventory_items WHERE inventory_id = @InventoryId";

        var rowsAffected = await connection.ExecuteAsync(sql, new { InventoryId = inventoryId });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Converts an InventoryEntity to an InventoryMessage
    /// </summary>
    public static InventoryMessage ToInventoryMessage(InventoryEntity entity, InventoryOperationType operationType = InventoryOperationType.Get)
    {
        return new InventoryMessage
        {
            InventoryId = entity.InventoryId,
            ProductId = entity.ProductId,
            Sku = entity.Sku,
            Location = entity.Location,
            QuantityInStock = entity.QuantityInStock,
            ReorderThreshold = entity.ReorderThreshold,
            OperationType = operationType
        };
    }

    /// <summary>
    /// Converts an InventoryMessage to an InventoryEntity
    /// </summary>
    public static InventoryEntity ToInventoryEntity(InventoryMessage message)
    {
        return new InventoryEntity
        {
            InventoryId = message.InventoryId ?? Guid.NewGuid().ToString(),
            ProductId = message.ProductId ?? string.Empty,
            Sku = message.Sku ?? string.Empty,
            Location = message.Location ?? string.Empty,
            QuantityInStock = message.QuantityInStock,
            ReorderThreshold = message.ReorderThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
