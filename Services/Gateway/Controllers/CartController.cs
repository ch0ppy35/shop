using Common.Messaging;
using Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ILogger<CartController> _logger;
    private readonly NatsService _natsService;

    public CartController(ILogger<CartController> logger, NatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        _logger.LogInformation("Received request to get cart");

        try
        {
            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var message = new CartMessage
            {
                OperationType = CartOperationType.GetCart,
                SessionId = sessionId
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<CartMessage, CartResponse>(
                "cart.get",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from cart service" });
            }

            if (!response.Success)
            {
                return StatusCode(400, new { error = response.Error ?? "Failed to get cart" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] CartItemDto item)
    {
        _logger.LogInformation("Received request to add item to cart: {ProductId}, Quantity: {Quantity}", item.ProductId, item.Quantity);

        try
        {
            // Validate the request
            if (string.IsNullOrEmpty(item.ProductId))
            {
                return BadRequest(new { error = "Product ID is required" });
            }

            if (item.Quantity <= 0)
            {
                return BadRequest(new { error = "Quantity must be greater than 0" });
            }

            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            // First, get the product details
            var productMessage = new ProductMessage
            {
                ProductId = item.ProductId,
                OperationType = ProductOperationType.Get,
                SessionId = sessionId
            };

            var productResponse = await _natsService.RequestAsync<ProductMessage, ProductResponse>(
                "products.get",
                productMessage,
                TimeSpan.FromSeconds(5));

            if (productResponse == null || !productResponse.Success || productResponse.Product == null)
            {
                return StatusCode(404, new { error = "Product not found" });
            }

            // Create the cart message
            var message = new CartMessage
            {
                OperationType = CartOperationType.AddItem,
                SessionId = sessionId,
                ProductId = item.ProductId,
                Name = productResponse.Product.Name,
                Price = productResponse.Product.Price,
                Quantity = item.Quantity
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<CartMessage, CartResponse>(
                "cart.additem",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from cart service" });
            }

            if (!response.Success)
            {
                return StatusCode(400, new { error = response.Error ?? "Failed to add item to cart" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPut("items/{productId}")]
    public async Task<IActionResult> UpdateItem(string productId, [FromBody] CartItemUpdateDto update)
    {
        _logger.LogInformation("Received request to update item in cart: {ProductId}, Quantity: {Quantity}", productId, update.Quantity);

        try
        {
            // Validate the request
            if (string.IsNullOrEmpty(productId))
            {
                return BadRequest(new { error = "Product ID is required" });
            }

            if (update.Quantity < 0)
            {
                return BadRequest(new { error = "Quantity cannot be negative" });
            }

            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var message = new CartMessage
            {
                OperationType = CartOperationType.UpdateItem,
                SessionId = sessionId,
                ProductId = productId,
                Quantity = update.Quantity
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<CartMessage, CartResponse>(
                "cart.updateitem",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from cart service" });
            }

            if (!response.Success)
            {
                return StatusCode(400, new { error = response.Error ?? "Failed to update item in cart" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item in cart");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(string productId)
    {
        _logger.LogInformation("Received request to remove item from cart: {ProductId}", productId);

        try
        {
            // Validate the request
            if (string.IsNullOrEmpty(productId))
            {
                return BadRequest(new { error = "Product ID is required" });
            }

            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var message = new CartMessage
            {
                OperationType = CartOperationType.RemoveItem,
                SessionId = sessionId,
                ProductId = productId
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<CartMessage, CartResponse>(
                "cart.removeitem",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from cart service" });
            }

            if (!response.Success)
            {
                return StatusCode(400, new { error = response.Error ?? "Failed to remove item from cart" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        _logger.LogInformation("Received request to clear cart");

        try
        {
            // Get the session ID from the HttpContext.Items
            var sessionId = HttpContext.Items["SessionId"]?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var message = new CartMessage
            {
                OperationType = CartOperationType.ClearCart,
                SessionId = sessionId
            };

            // Send request and wait for reply
            var response = await _natsService.RequestAsync<CartMessage, CartResponse>(
                "cart.clear",
                message,
                TimeSpan.FromSeconds(5));

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from cart service" });
            }

            if (!response.Success)
            {
                return StatusCode(400, new { error = response.Error ?? "Failed to clear cart" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }
}

public class CartItemDto
{
    public string? ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class CartItemUpdateDto
{
    public int Quantity { get; set; }
}
