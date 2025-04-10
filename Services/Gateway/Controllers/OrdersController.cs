using Common.Messaging;
using Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly NatsService _natsService;

    public OrdersController(ILogger<OrdersController> logger, NatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        _logger.LogInformation("Received request to get all orders");

        try
        {
            var message = new OrderMessage
            {
                OperationType = OrderOperationType.GetAll
            };

            await _natsService.PublishAsync("orders.getall", message);
            return Ok(new { message = "Request to get all orders has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        _logger.LogInformation("Received request to get order with ID: {OrderId}", id);

        try
        {
            var message = new OrderMessage
            {
                OrderId = id,
                OperationType = OrderOperationType.Get
            };

            await _natsService.PublishAsync("orders.get", message);
            return Ok(new { message = $"Request to get order {id} has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order with ID: {OrderId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderDto order)
    {
        _logger.LogInformation("Received request to create a new order");

        try
        {
            var message = new OrderMessage
            {
                OrderId = Guid.NewGuid().ToString(),
                CustomerId = order.CustomerId,
                Items = order.Items?.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList(),
                Status = OrderStatus.Created,
                OperationType = OrderOperationType.Create
            };

            await _natsService.PublishAsync("orders.create", message);
            return Ok(new { id = message.OrderId, message = "Order creation request has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(string id, [FromBody] OrderUpdateDto order)
    {
        _logger.LogInformation("Received request to update order with ID: {OrderId}", id);

        try
        {
            var message = new OrderMessage
            {
                OrderId = id,
                Status = order.Status,
                OperationType = OrderOperationType.Update
            };

            await _natsService.PublishAsync("orders.update", message);
            return Ok(new { message = $"Order update request for {id} has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order with ID: {OrderId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(string id)
    {
        _logger.LogInformation("Received request to delete order with ID: {OrderId}", id);

        try
        {
            var message = new OrderMessage
            {
                OrderId = id,
                OperationType = OrderOperationType.Delete
            };

            await _natsService.PublishAsync("orders.delete", message);
            return Ok(new { message = $"Order deletion request for {id} has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order with ID: {OrderId}", id);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }
}

public class OrderDto
{
    public string? CustomerId { get; set; }
    public List<OrderItemDto>? Items { get; set; }
}

public class OrderItemDto
{
    public string? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderUpdateDto
{
    public OrderStatus Status { get; set; }
}
