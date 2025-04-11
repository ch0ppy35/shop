using Common.Models;
using Inventory.Repositories;

namespace Inventory.Services;

/// <summary>
/// Service for managing inventory
/// </summary>
public class InventoryService
{
    private readonly ILogger<InventoryService> _logger;
    private readonly InventoryRepository _inventoryRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryService"/> class.
    /// </summary>
    public InventoryService(ILogger<InventoryService> logger, InventoryRepository inventoryRepository)
    {
        _logger = logger;
        _inventoryRepository = inventoryRepository;

        _logger.LogInformation("InventoryService initialized");
    }

    /// <summary>
    /// Gets all inventory items
    /// </summary>
    public async Task<IEnumerable<InventoryMessage>> GetAllInventoryAsync()
    {
        _logger.LogInformation("Getting all inventory items from database");
        var items = await _inventoryRepository.GetAllInventoryAsync();
        return items.Select(i => InventoryRepository.ToInventoryMessage(i));
    }

    /// <summary>
    /// Gets an inventory item by ID
    /// </summary>
    public async Task<InventoryMessage?> GetInventoryItemAsync(string id)
    {
        _logger.LogInformation("Getting inventory item with ID: {InventoryId}", id);

        var item = await _inventoryRepository.GetInventoryByIdAsync(id);
        if (item != null)
        {
            return InventoryRepository.ToInventoryMessage(item);
        }

        _logger.LogWarning("Inventory item with ID {InventoryId} not found", id);
        return null;
    }

    /// <summary>
    /// Creates a new inventory item
    /// </summary>
    public async Task<InventoryMessage> CreateInventoryItemAsync(InventoryMessage item)
    {
        _logger.LogInformation("Creating new inventory item with ID: {InventoryId}", item.InventoryId);

        if (string.IsNullOrEmpty(item.InventoryId))
        {
            item.InventoryId = Guid.NewGuid().ToString();
        }

        var entity = InventoryRepository.ToInventoryEntity(item);
        var createdItem = await _inventoryRepository.CreateInventoryAsync(entity);

        return InventoryRepository.ToInventoryMessage(createdItem, InventoryOperationType.Create);
    }

    /// <summary>
    /// Updates an existing inventory item
    /// </summary>
    public async Task<bool> UpdateInventoryItemAsync(InventoryMessage item)
    {
        if (string.IsNullOrEmpty(item.InventoryId))
        {
            _logger.LogWarning("Cannot update inventory item with null or empty ID");
            return false;
        }

        _logger.LogInformation("Updating inventory item with ID: {InventoryId}", item.InventoryId);

        var entity = InventoryRepository.ToInventoryEntity(item);
        return await _inventoryRepository.UpdateInventoryAsync(entity);
    }

    /// <summary>
    /// Deletes an inventory item
    /// </summary>
    public async Task<bool> DeleteInventoryItemAsync(string id)
    {
        _logger.LogInformation("Deleting inventory item with ID: {InventoryId}", id);
        return await _inventoryRepository.DeleteInventoryAsync(id);
    }
}
