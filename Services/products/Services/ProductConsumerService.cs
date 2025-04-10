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
            ConsumeCreateProductMessages(stoppingToken),
            ConsumeUpdateProductMessages(stoppingToken),
            ConsumeDeleteProductMessages(stoppingToken),
            ConsumeGetProductMessages(stoppingToken),
            ConsumeGetAllProductsMessages(stoppingToken)
        };

        // Wait for any task to complete (which should only happen on error or cancellation)
        await Task.WhenAny(tasks);

        _logger.LogWarning("One of the product consumer tasks has completed unexpectedly");
    }

    private async Task ConsumeCreateProductMessages(CancellationToken stoppingToken)
    {
        const string subject = "products.create";
        _logger.LogInformation("Starting to consume messages from subject: {Subject}", subject);

        try
        {
            await foreach (var message in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received create product message for product: {ProductName}", message.Name);
                
                try
                {
                    var product = _productService.CreateProduct(message);
                    _logger.LogInformation("Product created with ID: {ProductId}", product.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing create product message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Create product message consumption cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming create product messages");
        }
    }

    private async Task ConsumeUpdateProductMessages(CancellationToken stoppingToken)
    {
        const string subject = "products.update";
        _logger.LogInformation("Starting to consume messages from subject: {Subject}", subject);

        try
        {
            await foreach (var message in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received update product message for product ID: {ProductId}", message.ProductId);
                
                try
                {
                    var success = _productService.UpdateProduct(message);
                    if (success)
                    {
                        _logger.LogInformation("Product updated with ID: {ProductId}", message.ProductId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update product with ID: {ProductId}", message.ProductId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing update product message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update product message consumption cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming update product messages");
        }
    }

    private async Task ConsumeDeleteProductMessages(CancellationToken stoppingToken)
    {
        const string subject = "products.delete";
        _logger.LogInformation("Starting to consume messages from subject: {Subject}", subject);

        try
        {
            await foreach (var message in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received delete product message for product ID: {ProductId}", message.ProductId);
                
                try
                {
                    var success = _productService.DeleteProduct(message.ProductId!);
                    if (success)
                    {
                        _logger.LogInformation("Product deleted with ID: {ProductId}", message.ProductId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete product with ID: {ProductId}", message.ProductId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing delete product message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Delete product message consumption cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming delete product messages");
        }
    }

    private async Task ConsumeGetProductMessages(CancellationToken stoppingToken)
    {
        const string subject = "products.get";
        _logger.LogInformation("Starting to consume messages from subject: {Subject}", subject);

        try
        {
            await foreach (var message in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received get product message for product ID: {ProductId}", message.ProductId);
                
                try
                {
                    var product = _productService.GetProduct(message.ProductId!);
                    if (product != null)
                    {
                        _logger.LogInformation("Found product with ID: {ProductId}", message.ProductId);
                        // In a real implementation, we would publish the product back to a response subject
                    }
                    else
                    {
                        _logger.LogWarning("Product not found with ID: {ProductId}", message.ProductId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing get product message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Get product message consumption cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming get product messages");
        }
    }

    private async Task ConsumeGetAllProductsMessages(CancellationToken stoppingToken)
    {
        const string subject = "products.getall";
        _logger.LogInformation("Starting to consume messages from subject: {Subject}", subject);

        try
        {
            await foreach (var message in _natsService.SubscribeAsync<ProductMessage>(subject, stoppingToken))
            {
                _logger.LogInformation("Received get all products message");
                
                try
                {
                    var products = _productService.GetAllProducts();
                    _logger.LogInformation("Retrieved {Count} products", products.Count());
                    // In a real implementation, we would publish the products back to a response subject
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing get all products message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Get all products message consumption cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming get all products messages");
        }
    }
}
