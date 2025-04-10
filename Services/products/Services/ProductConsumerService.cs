using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Products.Services;

/// <summary>
/// Service for consuming product messages from NATS
/// </summary>
public class ProductConsumerService : BackgroundService
{
    private readonly ILogger<ProductConsumerService> _logger;
    private readonly NatsService _natsService;
    private readonly ProductService _productService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductConsumerService"/> class.
    /// </summary>
    public ProductConsumerService(
        ILogger<ProductConsumerService> logger,
        NatsService natsService,
        ProductService productService)
    {
        _logger = logger;
        _natsService = natsService;
        _productService = productService;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Product consumer service starting");

        // Start multiple consumers for different subjects
        var tasks = new List<Task>
        {
            HandleCreateProductRequests(stoppingToken),
            HandleUpdateProductRequests(stoppingToken),
            HandleDeleteProductRequests(stoppingToken),
            HandleGetProductRequests(stoppingToken),
            HandleGetAllProductsRequests(stoppingToken)
        };

        // Wait for any task to complete (which should only happen on error or cancellation)
        await Task.WhenAny(tasks);

        _logger.LogWarning("One of the product consumer tasks has completed unexpectedly");
    }

    private async Task HandleCreateProductRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.create";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received create product request for product: {ProductName}", msg.Name);

                // Prepare the response
                var response = new ProductResponse { Success = false };

                try
                {
                    // Create the product
                    var product = _productService.CreateProduct(msg);

                    // Set the response
                    response.Success = true;
                    response.Message = $"Product created with ID: {product.ProductId}";
                    response.Product = product;

                    _logger.LogInformation("Product created with ID: {ProductId}", product.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing create product request");
                    response.Error = $"Error creating product: {ex.Message}";
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
            _logger.LogInformation("Create product request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling create product requests");
        }
    }

    private async Task HandleUpdateProductRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.update";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received update product request for product ID: {ProductId}", msg.ProductId);

                // Prepare the response
                var response = new ProductResponse { Success = false };

                try
                {
                    // Update the product
                    var success = _productService.UpdateProduct(msg);

                    if (success)
                    {
                        // Get the updated product
                        var updatedProduct = _productService.GetProduct(msg.ProductId!);

                        // Set the response
                        response.Success = true;
                        response.Message = $"Product updated with ID: {msg.ProductId}";
                        response.Product = updatedProduct;

                        _logger.LogInformation("Product updated with ID: {ProductId}", msg.ProductId);
                    }
                    else
                    {
                        response.Error = $"Product with ID {msg.ProductId} not found";
                        _logger.LogWarning("Failed to update product with ID: {ProductId}", msg.ProductId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing update product request");
                    response.Error = $"Error updating product: {ex.Message}";
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
            _logger.LogInformation("Update product request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update product requests");
        }
    }

    private async Task HandleDeleteProductRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.delete";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received delete product request for product ID: {ProductId}", msg.ProductId);

                // Prepare the response
                var response = new BaseResponse { Success = false };

                try
                {
                    // Delete the product
                    var success = _productService.DeleteProduct(msg.ProductId!);

                    if (success)
                    {
                        // Set the response
                        response.Success = true;
                        response.Message = $"Product with ID {msg.ProductId} deleted successfully";

                        _logger.LogInformation("Product deleted with ID: {ProductId}", msg.ProductId);
                    }
                    else
                    {
                        response.Error = $"Product with ID {msg.ProductId} not found";
                        _logger.LogWarning("Failed to delete product with ID: {ProductId}", msg.ProductId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing delete product request");
                    response.Error = $"Error deleting product: {ex.Message}";
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
            _logger.LogInformation("Delete product request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling delete product requests");
        }
    }

    private async Task HandleGetProductRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.get";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received get product request for product ID: {ProductId}", msg.ProductId);

                // Prepare the response
                var response = new ProductResponse { Success = false };

                try
                {
                    // Get the product
                    var product = _productService.GetProduct(msg.ProductId!);

                    if (product != null)
                    {
                        // Set the response
                        response.Success = true;
                        response.Message = $"Product with ID {msg.ProductId} found";
                        response.Product = product;

                        _logger.LogInformation("Found product with ID: {ProductId}", msg.ProductId);
                    }
                    else
                    {
                        response.Error = $"Product with ID {msg.ProductId} not found";
                        _logger.LogWarning("Product not found with ID: {ProductId}", msg.ProductId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing get product request");
                    response.Error = $"Error getting product: {ex.Message}";
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
            _logger.LogInformation("Get product request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling get product requests");
        }
    }

    private async Task HandleGetAllProductsRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.getall";
        _logger.LogInformation("Starting to handle requests from subject: {Subject}", subject);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received get all products request");

                // Prepare the response
                var response = new ProductListResponse { Success = false };

                try
                {
                    // Get all products
                    var products = _productService.GetAllProducts().ToList();

                    // Set the response
                    response.Success = true;
                    response.Message = $"Retrieved {products.Count} products";
                    response.Products = products.ToList();

                    _logger.LogInformation("Retrieved {Count} products", products.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing get all products request");
                    response.Error = $"Error getting products: {ex.Message}";
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
            _logger.LogInformation("Get all products request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling get all products requests");
        }
    }
}
