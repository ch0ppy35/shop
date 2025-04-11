using Common.Messaging;
using Common.Models;

namespace Inventory.Services;

/// <summary>
/// Background service for consuming inventory-related NATS messages
/// </summary>
public class InventoryConsumerService : BackgroundService
{
    private readonly ILogger<InventoryConsumerService> _logger;
    private readonly NatsService _natsService;
    private readonly InventoryService _inventoryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryConsumerService"/> class.
    /// </summary>
    public InventoryConsumerService(
        ILogger<InventoryConsumerService> logger,
        NatsService natsService,
        InventoryService inventoryService)
    {
        _logger = logger;
        _natsService = natsService;
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inventory consumer service starting");

        // Start multiple consumers for different subjects
        var tasks = new List<Task>
        {
            HandleCreateInventoryRequests(stoppingToken),
            HandleUpdateInventoryRequests(stoppingToken),
            HandleDeleteInventoryRequests(stoppingToken),
            HandleGetInventoryRequests(stoppingToken),
            HandleGetAllInventoryRequests(stoppingToken)
        };

        // Wait for any task to complete (which should only happen on error or cancellation)
        await Task.WhenAny(tasks);

        _logger.LogWarning("One of the inventory consumer tasks has completed unexpectedly");
    }

    private async Task HandleCreateInventoryRequests(CancellationToken stoppingToken)
    {
        const string subject = "inventory.create";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<InventoryMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received create inventory request for item: {Sku} - SessionId: {SessionId}",
                    msg.Sku, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new InventoryResponse { Success = false };

                try
                {
                    // Create the inventory item
                    var createdItem = await _inventoryService.CreateInventoryItemAsync(msg);

                    // Set the response
                    response.Success = true;
                    response.Message = "Inventory item created successfully";
                    response.Inventory = createdItem;
                    response.SessionId = msg.SessionId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating inventory item");
                    response.Error = $"Error creating inventory item: {ex.Message}";
                    response.SessionId = msg.SessionId;
                }

                // Reply to the request if a reply subject is provided
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    try
                    {
                        await _natsService.PublishAsync(msg.ReplyTo, response, stoppingToken);
                        _logger.LogDebug("Sent response to {ReplyTo}", msg.ReplyTo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending response to {ReplyTo}", msg.ReplyTo);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Create inventory request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling create inventory requests");
        }
    }

    private async Task HandleUpdateInventoryRequests(CancellationToken stoppingToken)
    {
        const string subject = "inventory.update";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<InventoryMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received update inventory request for item ID: {InventoryId} - SessionId: {SessionId}",
                    msg.InventoryId, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new InventoryResponse { Success = false };

                try
                {
                    // Update the inventory item
                    var success = await _inventoryService.UpdateInventoryItemAsync(msg);

                    if (success)
                    {
                        // Get the updated item
                        var updatedItem = await _inventoryService.GetInventoryItemAsync(msg.InventoryId!);

                        // Set the response
                        response.Success = true;
                        response.Message = "Inventory item updated successfully";
                        response.Inventory = updatedItem;
                        response.SessionId = msg.SessionId;
                    }
                    else
                    {
                        response.Error = $"Inventory item with ID {msg.InventoryId} not found";
                        response.SessionId = msg.SessionId;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating inventory item with ID: {InventoryId}", msg.InventoryId);
                    response.Error = $"Error updating inventory item: {ex.Message}";
                    response.SessionId = msg.SessionId;
                }

                // Reply to the request if a reply subject is provided
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    try
                    {
                        await _natsService.PublishAsync(msg.ReplyTo, response, stoppingToken);
                        _logger.LogDebug("Sent response to {ReplyTo}", msg.ReplyTo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending response to {ReplyTo}", msg.ReplyTo);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update inventory request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update inventory requests");
        }
    }

    private async Task HandleDeleteInventoryRequests(CancellationToken stoppingToken)
    {
        const string subject = "inventory.delete";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<InventoryMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received delete inventory request for item ID: {InventoryId} - SessionId: {SessionId}",
                    msg.InventoryId, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new BaseResponse { Success = false };

                try
                {
                    // Delete the inventory item
                    var success = await _inventoryService.DeleteInventoryItemAsync(msg.InventoryId!);

                    if (success)
                    {
                        // Set the response
                        response.Success = true;
                        response.Message = $"Inventory item with ID {msg.InventoryId} deleted successfully";
                        response.SessionId = msg.SessionId;
                    }
                    else
                    {
                        response.Error = $"Inventory item with ID {msg.InventoryId} not found";
                        response.SessionId = msg.SessionId;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting inventory item with ID: {InventoryId}", msg.InventoryId);
                    response.Error = $"Error deleting inventory item: {ex.Message}";
                    response.SessionId = msg.SessionId;
                }

                // Reply to the request if a reply subject is provided
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    try
                    {
                        await _natsService.PublishAsync(msg.ReplyTo, response, stoppingToken);
                        _logger.LogDebug("Sent response to {ReplyTo}", msg.ReplyTo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending response to {ReplyTo}", msg.ReplyTo);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Delete inventory request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling delete inventory requests");
        }
    }

    private async Task HandleGetInventoryRequests(CancellationToken stoppingToken)
    {
        const string subject = "inventory.get";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<InventoryMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received get inventory request for item ID: {InventoryId} - SessionId: {SessionId}",
                    msg.InventoryId, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new InventoryResponse { Success = false };

                try
                {
                    // Get the inventory item
                    var item = await _inventoryService.GetInventoryItemAsync(msg.InventoryId!);

                    if (item != null)
                    {
                        // Set the response
                        response.Success = true;
                        response.Message = "Inventory item retrieved successfully";
                        response.Inventory = item;
                        response.SessionId = msg.SessionId;
                    }
                    else
                    {
                        response.Error = $"Inventory item with ID {msg.InventoryId} not found";
                        response.SessionId = msg.SessionId;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting inventory item with ID: {InventoryId}", msg.InventoryId);
                    response.Error = $"Error getting inventory item: {ex.Message}";
                    response.SessionId = msg.SessionId;
                }

                // Reply to the request if a reply subject is provided
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    try
                    {
                        await _natsService.PublishAsync(msg.ReplyTo, response, stoppingToken);
                        _logger.LogDebug("Sent response to {ReplyTo}", msg.ReplyTo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending response to {ReplyTo}", msg.ReplyTo);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Get inventory request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling get inventory requests");
        }
    }

    private async Task HandleGetAllInventoryRequests(CancellationToken stoppingToken)
    {
        const string subject = "inventory.getall";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<InventoryMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received get all inventory request - SessionId: {SessionId}",
                    msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new InventoryListResponse { Success = false };

                try
                {
                    // Get all inventory items
                    var items = (await _inventoryService.GetAllInventoryAsync()).ToList();

                    // Set the response
                    response.Success = true;
                    response.Message = $"Retrieved {items.Count} inventory items";
                    response.Inventory = items;
                    response.SessionId = msg.SessionId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting all inventory items");
                    response.Error = $"Error getting all inventory items: {ex.Message}";
                    response.SessionId = msg.SessionId;
                }

                // Reply to the request if a reply subject is provided
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    try
                    {
                        await _natsService.PublishAsync(msg.ReplyTo, response, stoppingToken);
                        _logger.LogDebug("Sent response to {ReplyTo}", msg.ReplyTo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending response to {ReplyTo}", msg.ReplyTo);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Get all inventory request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling get all inventory requests");
        }
    }
}
