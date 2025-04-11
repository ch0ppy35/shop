using Common.Messaging;
using Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ILogger<InventoryController> _logger;
    private readonly NatsService _natsService;

    public InventoryController(ILogger<InventoryController> logger, NatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory()
    {
        _logger.LogInformation("Received request to get all inventory items");

        try
        {
            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new InventoryMessage
            {
                OperationType = InventoryOperationType.GetAll,
                SessionId = sessionId
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<InventoryMessage, InventoryListResponse>(
                "inventory.getall",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from inventory service" });
            }

            if (!response.Success)
            {
                return StatusCode(500, new { error = response.Error ?? "An error occurred in the inventory service" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory items");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInventoryItem(string id)
    {
        _logger.LogInformation("Received request to get inventory item with ID: {InventoryId}", id);

        try
        {
            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new InventoryMessage
            {
                InventoryId = id,
                OperationType = InventoryOperationType.Get,
                SessionId = sessionId
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<InventoryMessage, InventoryResponse>(
                "inventory.get",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from inventory service" });
            }

            if (!response.Success)
            {
                return StatusCode(404, new { error = response.Error ?? $"Inventory item with ID {id} not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory item with ID: {InventoryId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateInventoryItem([FromBody] InventoryDto item)
    {
        _logger.LogInformation("Received request to create a new inventory item");

        try
        {
            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new InventoryMessage
            {
                InventoryId = Guid.NewGuid().ToString(),
                ProductId = item.ProductId,
                Sku = item.Sku,
                Location = item.Location,
                QuantityInStock = item.QuantityInStock,
                ReorderThreshold = item.ReorderThreshold,
                OperationType = InventoryOperationType.Create,
                SessionId = sessionId
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<InventoryMessage, InventoryResponse>(
                "inventory.create",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from inventory service" });
            }

            if (!response.Success)
            {
                return StatusCode(400, new { error = response.Error ?? "Failed to create inventory item" });
            }

            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory item");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInventoryItem(string id, [FromBody] InventoryDto item)
    {
        _logger.LogInformation("Received request to update inventory item with ID: {InventoryId}", id);

        try
        {
            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new InventoryMessage
            {
                InventoryId = id,
                ProductId = item.ProductId,
                Sku = item.Sku,
                Location = item.Location,
                QuantityInStock = item.QuantityInStock,
                ReorderThreshold = item.ReorderThreshold,
                OperationType = InventoryOperationType.Update,
                SessionId = sessionId
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<InventoryMessage, InventoryResponse>(
                "inventory.update",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from inventory service" });
            }

            if (!response.Success)
            {
                return StatusCode(404, new { error = response.Error ?? $"Inventory item with ID {id} not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item with ID: {InventoryId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInventoryItem(string id)
    {
        _logger.LogInformation("Received request to delete inventory item with ID: {InventoryId}", id);

        try
        {
            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new InventoryMessage
            {
                InventoryId = id,
                OperationType = InventoryOperationType.Delete,
                SessionId = sessionId
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<InventoryMessage, BaseResponse>(
                "inventory.delete",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from inventory service" });
            }

            if (!response.Success)
            {
                return StatusCode(404, new { error = response.Error ?? $"Inventory item with ID {id} not found" });
            }

            return Ok(new { success = true, message = response.Message ?? $"Inventory item with ID {id} deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item with ID: {InventoryId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }
}

public class InventoryDto
{
    public string? ProductId { get; set; }
    public string? Sku { get; set; }
    public string? Location { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderThreshold { get; set; }
}
