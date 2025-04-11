using Common.Models;
using Microsoft.Extensions.Logging;

namespace Inventory.Services;

/// <summary>
/// Service for managing inventory
/// </summary>
public class InventoryService
{
    private readonly ILogger<InventoryService> _logger;
    private readonly Dictionary<string, InventoryMessage> _inventory = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryService"/> class.
    /// </summary>
    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
        
        // Add some sample inventory items
        var item1 = new InventoryMessage
        {
            InventoryId = Guid.NewGuid().ToString(),
            ProductId = "product-1",
            Sku = "SKU-001",
            Location = "Warehouse A",
            QuantityInStock = 150,
            ReorderThreshold = 20,
            OperationType = InventoryOperationType.Create
        };
        
        var item2 = new InventoryMessage
        {
            InventoryId = Guid.NewGuid().ToString(),
            ProductId = "product-2",
            Sku = "SKU-002",
            Location = "Warehouse B",
            QuantityInStock = 75,
            ReorderThreshold = 15,
            OperationType = InventoryOperationType.Create
        };
        
        var item3 = new InventoryMessage
        {
            InventoryId = Guid.NewGuid().ToString(),
            ProductId = "product-3",
            Sku = "SKU-003",
            Location = "Warehouse A",
            QuantityInStock = 200,
            ReorderThreshold = 30,
            OperationType = InventoryOperationType.Create
        };
        
        _inventory.Add(item1.InventoryId!, item1);
        _inventory.Add(item2.InventoryId!, item2);
        _inventory.Add(item3.InventoryId!, item3);
        
        _logger.LogInformation("InventoryService initialized with {Count} sample inventory items", _inventory.Count);
    }

    /// <summary>
    /// Gets all inventory items
    /// </summary>
    public IEnumerable<InventoryMessage> GetAllInventory()
    {
        _logger.LogInformation("Getting all inventory items, count: {Count}", _inventory.Count);
        return _inventory.Values;
    }

    /// <summary>
    /// Gets an inventory item by ID
    /// </summary>
    public InventoryMessage? GetInventoryItem(string id)
    {
        _logger.LogInformation("Getting inventory item with ID: {InventoryId}", id);
        
        if (_inventory.TryGetValue(id, out var item))
        {
            return item;
        }
        
        _logger.LogWarning("Inventory item with ID {InventoryId} not found", id);
        return null;
    }

    /// <summary>
    /// Creates a new inventory item
    /// </summary>
    public InventoryMessage CreateInventoryItem(InventoryMessage item)
    {
        _logger.LogInformation("Creating new inventory item with ID: {InventoryId}", item.InventoryId);
        
        if (string.IsNullOrEmpty(item.InventoryId))
        {
            item.InventoryId = Guid.NewGuid().ToString();
        }
        
        _inventory[item.InventoryId] = item;
        return item;
    }

    /// <summary>
    /// Updates an existing inventory item
    /// </summary>
    public bool UpdateInventoryItem(InventoryMessage item)
    {
        if (string.IsNullOrEmpty(item.InventoryId))
        {
            _logger.LogWarning("Cannot update inventory item with null or empty ID");
            return false;
        }
        
        _logger.LogInformation("Updating inventory item with ID: {InventoryId}", item.InventoryId);
        
        if (_inventory.ContainsKey(item.InventoryId))
        {
            _inventory[item.InventoryId] = item;
            return true;
        }
        
        _logger.LogWarning("Inventory item with ID {InventoryId} not found for update", item.InventoryId);
        return false;
    }

    /// <summary>
    /// Deletes an inventory item
    /// </summary>
    public bool DeleteInventoryItem(string id)
    {
        _logger.LogInformation("Deleting inventory item with ID: {InventoryId}", id);
        
        if (_inventory.ContainsKey(id))
        {
            _inventory.Remove(id);
            return true;
        }
        
        _logger.LogWarning("Inventory item with ID {InventoryId} not found for deletion", id);
        return false;
    }
}
