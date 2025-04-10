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
    public async Task<IActionResult> GetProducts()
    {
        _logger.LogInformation("Received request to get all products");

        try
        {
            var message = new ProductMessage
            {
                OperationType = ProductOperationType.GetAll
            };

            await _natsService.PublishAsync("products.getall", message);
            return Ok(new { message = "Request to get all products has been sent" });
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
            var message = new ProductMessage
            {
                ProductId = id,
                OperationType = ProductOperationType.Get
            };

            await _natsService.PublishAsync("products.get", message);
            return Ok(new { message = $"Request to get product {id} has been sent" });
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
            var message = new ProductMessage
            {
                ProductId = Guid.NewGuid().ToString(),
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                OperationType = ProductOperationType.Create
            };

            await _natsService.PublishAsync("products.create", message);
            return Ok(new { id = message.ProductId, message = "Product creation request has been sent" });
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
            var message = new ProductMessage
            {
                ProductId = id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                OperationType = ProductOperationType.Update
            };

            await _natsService.PublishAsync("products.update", message);
            return Ok(new { message = $"Product update request for {id} has been sent" });
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
            var message = new ProductMessage
            {
                ProductId = id,
                OperationType = ProductOperationType.Delete
            };

            await _natsService.PublishAsync("products.delete", message);
            return Ok(new { message = $"Product deletion request for {id} has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }
}

public class ProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
