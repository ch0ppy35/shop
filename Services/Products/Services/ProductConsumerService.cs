using Common.Messaging;
using Common.Models;

namespace Products.Services;

/// <summary>
/// Service for consuming product messages from NATS
/// </summary>
public class ProductConsumerService : BackgroundService
{
    private readonly ILogger<ProductConsumerService> _logger;
    private readonly INatsService _natsService;
    private readonly IProductService _productService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductConsumerService"/> class.
    /// </summary>
    public ProductConsumerService(
        ILogger<ProductConsumerService> logger,
        INatsService natsService,
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

        await WaitForNatsConnectionAsync(stoppingToken);

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

        await Task.WhenAny(tasks);

        _logger.LogWarning("One of the product consumer tasks has completed unexpectedly");
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

    private async Task HandleCreateProductRequests(CancellationToken stoppingToken)
    {
        const string subject = "products.create";
        const string queueGroup = "products-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}", subject, queueGroup);

        try
        {
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received create product request for product: {ProductName} - SessionId: {SessionId}", msg.Name, msg.SessionId ?? "Unknown");

                var response = new ProductResponse { Success = false };

                try
                {
                    var product = await _productService.CreateProductAsync(msg);

                    response.Success = true;
                    response.Message = $"Product created with ID: {product.ProductId}";
                    response.Product = product;

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
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received update product request for product ID: {ProductId} - SessionId: {SessionId}", msg.ProductId, msg.SessionId ?? "Unknown");

                var response = new ProductResponse { Success = false };

                try
                {
                    var success = await _productService.UpdateProductAsync(msg);

                    if (success)
                    {
                        var updatedProduct = await _productService.GetProductAsync(msg.ProductId!);

                        response.Success = true;
                        response.Message = $"Product updated with ID: {msg.ProductId}";
                        response.Product = updatedProduct;

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
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received delete product request for product ID: {ProductId} - SessionId: {SessionId}", msg.ProductId, msg.SessionId ?? "Unknown");

                var response = new BaseResponse { Success = false };

                try
                {
                    var success = await _productService.DeleteProductAsync(msg.ProductId!);

                    if (success)
                    {
                        response.Success = true;
                        response.Message = $"Product with ID {msg.ProductId} deleted successfully";

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
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received get product request for product ID: {ProductId} - SessionId: {SessionId}", msg.ProductId, msg.SessionId ?? "Unknown");

                var response = new ProductResponse { Success = false };

                try
                {
                    var product = await _productService.GetProductAsync(msg.ProductId!);

                    if (product != null)
                    {
                        response.Success = true;
                        response.Message = $"Product with ID {msg.ProductId} found";
                        response.Product = product;

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
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received get all products request - SessionId: {SessionId}", msg.SessionId ?? "Unknown");

                var response = new ProductListResponse { Success = false };

                try
                {
                    int pageNumber = Math.Max(1, msg.PageNumber);
                    int pageSize = Math.Clamp(msg.PageSize, 1, 100);

                    _logger.LogInformation("Processing get all products request with pagination: Page {PageNumber}, Size {PageSize}",
                        pageNumber, pageSize);

                    var (products, totalCount, totalPages) = await _productService.GetPaginatedProductsAsync(pageNumber, pageSize);
                    var productsList = products.ToList();

                    response.Success = true;
                    response.Message = $"Retrieved {productsList.Count} products (page {pageNumber} of {totalPages})";
                    response.Products = productsList;
                    response.TotalCount = totalCount;
                    response.PageNumber = pageNumber;
                    response.PageSize = pageSize;
                    response.TotalPages = totalPages;
                    response.HasPreviousPage = pageNumber > 1;
                    response.HasNextPage = pageNumber < totalPages;

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
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received get inventory request for product ID: {ProductId} - SessionId: {SessionId}",
                    msg.ProductId, msg.SessionId ?? "Unknown");

                var response = new ProductResponse { Success = false };

                try
                {
                    var product = await _productService.GetInventoryAsync(msg.ProductId ?? string.Empty);

                    if (product != null)
                    {
                        response.Success = true;
                        response.Message = $"Retrieved inventory for product {product.ProductId}";
                        response.Product = product;

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
            await foreach (var msg in _natsService.SubscribeAsync<ProductMessage>(subject, queueGroup, stoppingToken))
            {
                _logger.LogInformation("Received update inventory request for product ID: {ProductId} - SessionId: {SessionId}",
                    msg.ProductId, msg.SessionId ?? "Unknown");

                var response = new ProductResponse { Success = false };

                try
                {
                    var success = await _productService.UpdateInventoryAsync(msg.ProductId ?? string.Empty, msg.QuantityInStock);

                    if (success)
                    {
                        var updatedProduct = await _productService.GetInventoryAsync(msg.ProductId ?? string.Empty);

                        response.Success = true;
                        response.Message = $"Updated inventory for product {msg.ProductId}";
                        response.Product = updatedProduct;

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
