using Common.Messaging;
using Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;
    private readonly NatsService _natsService;

    public ProductsController(ILogger<ProductsController> logger, NatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Received request to get products: Page {Page}, PageSize {PageSize}", page, pageSize);

        try
        {

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;


            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new ProductMessage
            {
                OperationType = ProductOperationType.GetAll,
                SessionId = sessionId,
                PageNumber = page,
                PageSize = pageSize
            };


            var response = await _natsService.RequestAsync<ProductMessage, ProductListResponse>(
                "products.getall",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from products service" });
            }

            if (!response.Success)
            {
                return StatusCode(500, new { error = response.Error ?? "An error occurred in the products service" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(string id)
    {
        _logger.LogInformation("Received request to get product with ID: {ProductId}", id);

        try
        {

            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new ProductMessage
            {
                ProductId = id,
                OperationType = ProductOperationType.Get,
                SessionId = sessionId
            };


            var response = await _natsService.RequestAsync<ProductMessage, ProductResponse>(
                "products.get",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from products service" });
            }

            if (!response.Success)
            {
                return StatusCode(404, new { error = response.Error ?? $"Product with ID {id} not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product with ID: {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductDto product)
    {
        _logger.LogInformation("Received request to create a new product");

        try
        {

            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new ProductMessage
            {
                ProductId = Guid.NewGuid().ToString(),
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,

                Sku = product.Sku,
                Location = product.Location,
                QuantityInStock = product.QuantityInStock,
                ReorderThreshold = product.ReorderThreshold,
                OperationType = ProductOperationType.Create,
                SessionId = sessionId
            };


            var response = await _natsService.RequestAsync<ProductMessage, ProductResponse>(
                "products.create",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from products service" });
            }

            if (!response.Success)
            {
                return StatusCode(400, new { error = response.Error ?? "Failed to create product" });
            }

            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] ProductDto product)
    {
        _logger.LogInformation("Received request to update product with ID: {ProductId}", id);

        try
        {

            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new ProductMessage
            {
                ProductId = id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,

                Sku = product.Sku,
                Location = product.Location,
                QuantityInStock = product.QuantityInStock,
                ReorderThreshold = product.ReorderThreshold,
                OperationType = ProductOperationType.Update,
                SessionId = sessionId
            };


            var response = await _natsService.RequestAsync<ProductMessage, ProductResponse>(
                "products.update",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from products service" });
            }

            if (!response.Success)
            {
                return StatusCode(404, new { error = response.Error ?? $"Product with ID {id} not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        _logger.LogInformation("Received request to delete product with ID: {ProductId}", id);

        try
        {

            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new ProductMessage
            {
                ProductId = id,
                OperationType = ProductOperationType.Delete,
                SessionId = sessionId
            };


            var response = await _natsService.RequestAsync<ProductMessage, BaseResponse>(
                "products.delete",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from products service" });
            }

            if (!response.Success)
            {
                return StatusCode(404, new { error = response.Error ?? $"Product with ID {id} not found" });
            }

            return Ok(new { success = true, message = response.Message ?? $"Product with ID {id} deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpGet("{id}/inventory")]
    public async Task<IActionResult> GetProductInventory(string id)
    {
        _logger.LogInformation("Received request to get inventory for product with ID: {ProductId}", id);

        try
        {

            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new ProductMessage
            {
                ProductId = id,
                OperationType = ProductOperationType.GetInventory,
                SessionId = sessionId
            };


            var response = await _natsService.RequestAsync<ProductMessage, ProductResponse>(
                "products.inventory.get",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from products service" });
            }

            if (!response.Success)
            {
                return StatusCode(404, new { error = response.Error ?? $"Inventory for product with ID {id} not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory for product with ID: {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPut("{id}/inventory")]
    public async Task<IActionResult> UpdateProductInventory(string id, [FromBody] InventoryUpdateDto item)
    {
        _logger.LogInformation("Received request to update inventory for product with ID: {ProductId}", id);

        try
        {

            var sessionId = HttpContext.Items["SessionId"]?.ToString();

            var message = new ProductMessage
            {
                ProductId = id,
                Sku = item.Sku,
                Location = item.Location,
                QuantityInStock = item.QuantityInStock,
                ReorderThreshold = item.ReorderThreshold,
                OperationType = ProductOperationType.UpdateInventory,
                SessionId = sessionId
            };


            var response = await _natsService.RequestAsync<ProductMessage, ProductResponse>(
                "products.inventory.update",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from products service" });
            }

            if (!response.Success)
            {
                return StatusCode(404, new { error = response.Error ?? $"Failed to update inventory for product with ID {id}" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory for product with ID: {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }
}

public class ProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }

    public string? Sku { get; set; }
    public string? Location { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderThreshold { get; set; }
}

public class InventoryUpdateDto
{
    public string? Sku { get; set; }
    public string? Location { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderThreshold { get; set; }
}
