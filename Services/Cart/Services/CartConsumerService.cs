using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cart.Services;

/// <summary>
/// Hosted service for consuming cart-related messages from NATS
/// </summary>
public class CartConsumerService : BackgroundService
{
    private readonly ILogger<CartConsumerService> _logger;
    private readonly INatsService _natsService;
    private readonly CartService _cartService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartConsumerService"/> class.
    /// </summary>
    public CartConsumerService(
        ILogger<CartConsumerService> logger,
        INatsService natsService,
        CartService cartService)
    {
        _logger = logger;
        _natsService = natsService;
        _cartService = cartService;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cart consumer service starting");

        // Wait for NATS connection to be established before starting consumers
        await WaitForNatsConnectionAsync(stoppingToken);

        // Start all message handlers
        var tasks = new List<Task>
        {
            HandleGetCartRequests(stoppingToken),
            HandleAddItemRequests(stoppingToken),
            HandleUpdateItemRequests(stoppingToken),
            HandleRemoveItemRequests(stoppingToken),
            HandleClearCartRequests(stoppingToken)
        };

        // Wait for any task to complete (which should only happen on error or cancellation)
        await Task.WhenAny(tasks);

        _logger.LogWarning("One of the cart message handlers has stopped unexpectedly");
    }

    /// <summary>
    /// Waits for the NATS connection to be established before proceeding
    /// </summary>
    private async Task WaitForNatsConnectionAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 60; // Maximum number of retries (10 minutes with 10-second delay)
        const int retryDelaySeconds = 10; // Delay between retries
        int retryCount = 0;

        while (!_natsService.IsConnected && retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Waiting for NATS connection to be established (attempt {RetryCount}/{MaxRetries})...",
                retryCount + 1, maxRetries);

            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);
            retryCount++;
        }

        if (_natsService.IsConnected)
        {
            _logger.LogInformation("NATS connection established, starting message handlers");
        }
        else if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("Waiting for NATS connection was cancelled");
        }
        else
        {
            _logger.LogError("Failed to establish NATS connection after {MaxRetries} retries", maxRetries);
        }
    }

    private async Task HandleGetCartRequests(CancellationToken stoppingToken)
    {
        const string subject = "cart.get";
        const string queueGroup = "cart-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<CartMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received get cart request - SessionId: {SessionId}", msg.SessionId ?? "Unknown");

                // Ensure we have a session ID
                if (string.IsNullOrEmpty(msg.SessionId))
                {
                    _logger.LogWarning("Get cart request missing session ID");
                    continue;
                }

                // Get the cart
                var response = await _cartService.GetCartAsync(msg.SessionId, stoppingToken);

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
            _logger.LogInformation("Get cart request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling get cart requests");
        }
    }

    private async Task HandleAddItemRequests(CancellationToken stoppingToken)
    {
        const string subject = "cart.additem";
        const string queueGroup = "cart-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<CartMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received add item request - SessionId: {SessionId}, ProductId: {ProductId}, Quantity: {Quantity}",
                    msg.SessionId ?? "Unknown", msg.ProductId ?? "Unknown", msg.Quantity);

                // Ensure we have a session ID and product ID
                if (string.IsNullOrEmpty(msg.SessionId) || string.IsNullOrEmpty(msg.ProductId))
                {
                    _logger.LogWarning("Add item request missing session ID or product ID");
                    continue;
                }

                // Create the cart item
                var cartItem = new CartItem
                {
                    ProductId = msg.ProductId,
                    Name = msg.Name,
                    Price = msg.Price,
                    Quantity = msg.Quantity
                };

                // Add the item to the cart
                var response = await _cartService.AddItemAsync(msg.SessionId, cartItem, stoppingToken);

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
            _logger.LogInformation("Add item request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling add item requests");
        }
    }

    private async Task HandleUpdateItemRequests(CancellationToken stoppingToken)
    {
        const string subject = "cart.updateitem";
        const string queueGroup = "cart-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<CartMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received update item request - SessionId: {SessionId}, ProductId: {ProductId}, Quantity: {Quantity}",
                    msg.SessionId ?? "Unknown", msg.ProductId ?? "Unknown", msg.Quantity);

                // Ensure we have a session ID and product ID
                if (string.IsNullOrEmpty(msg.SessionId) || string.IsNullOrEmpty(msg.ProductId))
                {
                    _logger.LogWarning("Update item request missing session ID or product ID");
                    continue;
                }

                // Update the item in the cart
                var response = await _cartService.UpdateItemAsync(msg.SessionId, msg.ProductId, msg.Quantity, stoppingToken);

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
            _logger.LogInformation("Update item request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update item requests");
        }
    }

    private async Task HandleRemoveItemRequests(CancellationToken stoppingToken)
    {
        const string subject = "cart.removeitem";
        const string queueGroup = "cart-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<CartMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received remove item request - SessionId: {SessionId}, ProductId: {ProductId}",
                    msg.SessionId ?? "Unknown", msg.ProductId ?? "Unknown");

                // Ensure we have a session ID and product ID
                if (string.IsNullOrEmpty(msg.SessionId) || string.IsNullOrEmpty(msg.ProductId))
                {
                    _logger.LogWarning("Remove item request missing session ID or product ID");
                    continue;
                }

                // Remove the item from the cart
                var response = await _cartService.RemoveItemAsync(msg.SessionId, msg.ProductId, stoppingToken);

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
            _logger.LogInformation("Remove item request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling remove item requests");
        }
    }

    private async Task HandleClearCartRequests(CancellationToken stoppingToken)
    {
        const string subject = "cart.clear";
        const string queueGroup = "cart-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<CartMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received clear cart request - SessionId: {SessionId}", msg.SessionId ?? "Unknown");

                // Ensure we have a session ID
                if (string.IsNullOrEmpty(msg.SessionId))
                {
                    _logger.LogWarning("Clear cart request missing session ID");
                    continue;
                }

                // Clear the cart
                var response = await _cartService.ClearCartAsync(msg.SessionId, stoppingToken);

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
            _logger.LogInformation("Clear cart request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling clear cart requests");
        }
    }
}
