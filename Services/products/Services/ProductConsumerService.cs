using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Products.Services;

/// <summary>
/// Service for consuming product messages from NATS
/// </summary>
public class ProductConsumerService : BackgroundService
{
    private readonly ILogger<ProductConsumerService> _logger;
    private readonly NatsService _natsService;
    private readonly IProductService _productService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductConsumerService"/> class.
    /// </summary>
    public ProductConsumerService(
        ILogger<ProductConsumerService> logger,
        NatsService natsService,
        IProductService productService)
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
            HandleGetAllProductsRequests(stoppingToken),
            HandleGetInventoryRequests(stoppingToken),
            HandleUpdateInventoryRequests(stoppingToken)
        };

        // Wait for any task to complete (which should only happen on error or cancellation)
        await Task.WhenAny(tasks);

        _logger.LogWarning("One of the product consumer tasks has completed unexpectedly");
    }

    private async Task HandleCreateProductRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.create";
        const string queueGroup = "products-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received create product request for product: {ProductName} - SessionId: {SessionId}", msg.Name, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new ProductResponse { Success = false };

                try
                {
                    // Create the product
                    var product = await _productService.CreateProductAsync(msg);

                    // Set the response
                    response.Success = true;
                    response.Message = $"Product created with ID: {product.ProductId}";
                    response.Product = product;

                    // Preserve the session ID in the response
                    if (!string.IsNullOrEmpty(msg.SessionId))
                    {
                        response.SessionId = msg.SessionId;
                    }

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
        const string queueGroup = "products-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received update product request for product ID: {ProductId} - SessionId: {SessionId}", msg.ProductId, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new ProductResponse { Success = false };

                try
                {
                    // Update the product
                    var success = await _productService.UpdateProductAsync(msg);

                    if (success)
                    {
                        // Get the updated product
                        var updatedProduct = await _productService.GetProductAsync(msg.ProductId!);

                        // Set the response
                        response.Success = true;
                        response.Message = $"Product updated with ID: {msg.ProductId}";
                        response.Product = updatedProduct;

                        // Preserve the session ID in the response
                        if (!string.IsNullOrEmpty(msg.SessionId))
                        {
                            response.SessionId = msg.SessionId;
                        }

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
        const string queueGroup = "products-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received delete product request for product ID: {ProductId} - SessionId: {SessionId}", msg.ProductId, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new BaseResponse { Success = false };

                try
                {
                    // Delete the product
                    var success = await _productService.DeleteProductAsync(msg.ProductId!);

                    if (success)
                    {
                        // Set the response
                        response.Success = true;
                        response.Message = $"Product with ID {msg.ProductId} deleted successfully";

                        // Preserve the session ID in the response
                        if (!string.IsNullOrEmpty(msg.SessionId))
                        {
                            response.SessionId = msg.SessionId;
                        }

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
        const string queueGroup = "products-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received get product request for product ID: {ProductId} - SessionId: {SessionId}", msg.ProductId, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new ProductResponse { Success = false };

                try
                {
                    // Get the product
                    var product = await _productService.GetProductAsync(msg.ProductId!);

                    if (product != null)
                    {
                        // Set the response
                        response.Success = true;
                        response.Message = $"Product with ID {msg.ProductId} found";
                        response.Product = product;

                        // Preserve the session ID in the response
                        if (!string.IsNullOrEmpty(msg.SessionId))
                        {
                            response.SessionId = msg.SessionId;
                        }

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
        const string queueGroup = "products-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received get all products request - SessionId: {SessionId}", msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new ProductListResponse { Success = false };

                try
                {
                    // Validate pagination parameters
                    int pageNumber = Math.Max(1, msg.PageNumber);
                    int pageSize = Math.Clamp(msg.PageSize, 1, 100);

                    _logger.LogInformation("Processing get all products request with pagination: Page {PageNumber}, Size {PageSize}",
                        pageNumber, pageSize);

                    // Get paginated products
                    var (products, totalCount, totalPages) = await _productService.GetPaginatedProductsAsync(pageNumber, pageSize);
                    var productsList = products.ToList();

                    // Set the response with pagination metadata
                    response.Success = true;
                    response.Message = $"Retrieved {productsList.Count} products (page {pageNumber} of {totalPages})";
                    response.Products = productsList;
                    response.TotalCount = totalCount;
                    response.PageNumber = pageNumber;
                    response.PageSize = pageSize;
                    response.TotalPages = totalPages;
                    response.HasPreviousPage = pageNumber > 1;
                    response.HasNextPage = pageNumber < totalPages;

                    // Preserve the session ID in the response
                    if (!string.IsNullOrEmpty(msg.SessionId))
                    {
                        response.SessionId = msg.SessionId;
                    }

                    _logger.LogInformation("Retrieved {Count} products (page {PageNumber} of {TotalPages}, total: {TotalCount})",
                        productsList.Count, pageNumber, totalPages, totalCount);
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

    private async Task HandleGetInventoryRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.inventory.get";
        const string queueGroup = "products-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received get inventory request for product ID: {ProductId} - SessionId: {SessionId}",
                    msg.ProductId, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new ProductResponse { Success = false };

                try
                {
                    // Get the product with inventory information
                    var product = await _productService.GetInventoryAsync(msg.ProductId ?? string.Empty);

                    if (product != null)
                    {
                        // Set the response
                        response.Success = true;
                        response.Message = $"Retrieved inventory for product {product.ProductId}";
                        response.Product = product;

                        // Preserve the session ID in the response
                        if (!string.IsNullOrEmpty(msg.SessionId))
                        {
                            response.SessionId = msg.SessionId;
                        }

                        _logger.LogInformation("Found inventory for product with ID: {ProductId}", msg.ProductId);
                    }
                    else
                    {
                        response.Error = $"Product with ID {msg.ProductId} not found";
                        _logger.LogWarning("Product not found with ID: {ProductId}", msg.ProductId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing get inventory request");
                    response.Error = $"Error getting inventory: {ex.Message}";
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

    private async Task HandleUpdateInventoryRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.inventory.update";
        const string queueGroup = "products-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            // Subscribe to the subject and handle each request
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received update inventory request for product ID: {ProductId} - SessionId: {SessionId}",
                    msg.ProductId, msg.SessionId ?? "Unknown");

                // Prepare the response
                var response = new ProductResponse { Success = false };

                try
                {
                    // Update the inventory
                    var success = await _productService.UpdateInventoryAsync(msg.ProductId ?? string.Empty, msg.QuantityInStock);

                    if (success)
                    {
                        // Get the updated product to include in the response
                        var updatedProduct = await _productService.GetInventoryAsync(msg.ProductId ?? string.Empty);

                        // Set the response
                        response.Success = true;
                        response.Message = $"Updated inventory for product {msg.ProductId}";
                        response.Product = updatedProduct;

                        // Preserve the session ID in the response
                        if (!string.IsNullOrEmpty(msg.SessionId))
                        {
                            response.SessionId = msg.SessionId;
                        }

                        _logger.LogInformation("Updated inventory for product with ID: {ProductId}", msg.ProductId);
                    }
                    else
                    {
                        response.Error = $"Failed to update inventory for product with ID {msg.ProductId}";
                        _logger.LogWarning("Failed to update inventory for product with ID: {ProductId}", msg.ProductId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing update inventory request");
                    response.Error = $"Error updating inventory: {ex.Message}";
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
}
