using Common.Messaging;
using Common.Models;

namespace Products.Services;

/// <summary>
/// Service for consuming product messages from NATS
/// </summary>
/// <remarks>
/// Each subject gets its own subscription loop that handles up to <see cref="MaxConcurrency"/> messages at once.
/// Every message runs in its own DI scope, so it gets its own <c>ProductDbContext</c> from the pool; a DbContext
/// is not thread safe and must never be shared between concurrent handlers.
/// </remarks>
public class ProductConsumerService : BackgroundService
{
    /// <summary>
    /// The queue group every Products instance joins, so NATS load-balances requests across them
    /// </summary>
    public const string QueueGroup = "products-service";

    /// <summary>
    /// The most messages a single subject handles at the same time
    /// </summary>
    public const int MaxConcurrency = 16;

    /// <summary>
    /// The request subjects this service answers
    /// </summary>
    public static readonly IReadOnlyList<string> RequestSubjects = new[]
    {
        "products.create",
        "products.update",
        "products.delete",
        "products.get",
        "products.getall",
        "products.inventory.get",
        "products.inventory.update"
    };

    private readonly ILogger<ProductConsumerService> _logger;
    private readonly INatsService _natsService;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductConsumerService"/> class.
    /// </summary>
    public ProductConsumerService(
        ILogger<ProductConsumerService> logger,
        INatsService natsService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _natsService = natsService;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Product consumer service starting");

        await WaitForNatsConnectionAsync(stoppingToken);

        var tasks = RequestSubjects.Select(subject => ConsumeAsync(subject, stoppingToken)).ToList();

        await Task.WhenAny(tasks);

        if (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("One of the product consumer tasks has completed unexpectedly");
        }
    }

    /// <summary>
    /// Handles one request: runs it in a fresh scope, replies, and publishes a change event after a successful write
    /// </summary>
    /// <param name="subject">The subject the request arrived on</param>
    /// <param name="message">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response that was (or would be) sent to the requester</returns>
    public async Task<BaseResponse> HandleRequestAsync(string subject, ProductMessage message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received {Subject} request for product {ProductId} - SessionId: {SessionId}",
            subject, message.ProductId ?? "-", message.SessionId ?? "Unknown");

        BaseResponse response;
        ProductChangedEvent? change = null;

        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

            try
            {
                (response, change) = await DispatchAsync(subject, message, productService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {Subject} request", subject);
                response = CreateEmptyResponse(subject);
                response.Error = $"Error processing {subject}: {ex.Message}";
            }
        }

        response.SessionId = message.SessionId;

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            await ReplyAsync(message.ReplyTo, response, cancellationToken);
        }

        if (change != null)
        {
            await PublishChangeAsync(change, cancellationToken);
        }

        return response;
    }

    private static async Task<(BaseResponse Response, ProductChangedEvent? Change)> DispatchAsync(
        string subject, ProductMessage msg, IProductService productService)
    {
        if (subject != "products.getall" && subject != "products.create" && string.IsNullOrEmpty(msg.ProductId))
        {
            var invalid = CreateEmptyResponse(subject);
            invalid.Error = "Product ID cannot be null or empty";
            return (invalid, null);
        }

        switch (subject)
        {
            case "products.create":
            {
                var product = await productService.CreateProductAsync(msg);
                return (new ProductResponse
                    {
                        Success = true,
                        Message = $"Product created with ID: {product.ProductId}",
                        Product = product
                    },
                    Changed(product.ProductId!, ProductChangeType.Created, product));
            }

            case "products.update":
            {
                if (!await productService.UpdateProductAsync(msg))
                {
                    return (new ProductResponse { Error = $"Product with ID {msg.ProductId} not found" }, null);
                }

                var product = await productService.GetProductAsync(msg.ProductId!);
                return (new ProductResponse
                    {
                        Success = true,
                        Message = $"Product updated with ID: {msg.ProductId}",
                        Product = product
                    },
                    Changed(msg.ProductId!, ProductChangeType.Updated, product));
            }

            case "products.delete":
            {
                if (!await productService.DeleteProductAsync(msg.ProductId!))
                {
                    return (new BaseResponse { Error = $"Product with ID {msg.ProductId} not found" }, null);
                }

                return (new BaseResponse
                    {
                        Success = true,
                        Message = $"Product with ID {msg.ProductId} deleted successfully"
                    },
                    Changed(msg.ProductId!, ProductChangeType.Deleted, null));
            }

            case "products.get":
            {
                var product = await productService.GetProductAsync(msg.ProductId!);
                return product == null
                    ? (new ProductResponse { Error = $"Product with ID {msg.ProductId} not found" }, null)
                    : (new ProductResponse
                    {
                        Success = true,
                        Message = $"Product with ID {msg.ProductId} found",
                        Product = product
                    }, null);
            }

            case "products.getall":
            {
                var pageNumber = Math.Max(1, msg.PageNumber);
                var pageSize = Math.Clamp(msg.PageSize, 1, 100);

                var (products, totalCount, totalPages) =
                    await productService.GetPaginatedProductsAsync(pageNumber, pageSize);
                var productsList = products.ToList();

                return (new ProductListResponse
                {
                    Success = true,
                    Message = $"Retrieved {productsList.Count} products (page {pageNumber} of {totalPages})",
                    Products = productsList,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < totalPages
                }, null);
            }

            case "products.inventory.get":
            {
                var product = await productService.GetInventoryAsync(msg.ProductId!);
                return product == null
                    ? (new ProductResponse { Error = $"Product with ID {msg.ProductId} not found" }, null)
                    : (new ProductResponse
                    {
                        Success = true,
                        Message = $"Retrieved inventory for product {product.ProductId}",
                        Product = product
                    }, null);
            }

            case "products.inventory.update":
            {
                if (!await productService.UpdateInventoryAsync(msg.ProductId!, msg.QuantityInStock))
                {
                    return (new ProductResponse
                    {
                        Error = $"Failed to update inventory for product with ID {msg.ProductId}"
                    }, null);
                }

                var product = await productService.GetProductAsync(msg.ProductId!);
                return (new ProductResponse
                    {
                        Success = true,
                        Message = $"Updated inventory for product {msg.ProductId}",
                        Product = product
                    },
                    Changed(msg.ProductId!, ProductChangeType.InventoryUpdated, product));
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(subject), subject, "Unknown products subject");
        }
    }

    private static ProductChangedEvent Changed(string productId, ProductChangeType type, ProductMessage? product) =>
        new() { ProductId = productId, ChangeType = type, Product = product };

    private static BaseResponse CreateEmptyResponse(string subject) => subject switch
    {
        "products.getall" => new ProductListResponse(),
        "products.delete" => new BaseResponse(),
        _ => new ProductResponse()
    };

    private async Task ConsumeAsync(string subject, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}",
            subject, QueueGroup);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrency,
            CancellationToken = stoppingToken
        };

        try
        {
            await Parallel.ForEachAsync(
                _natsService.SubscribeAsync<ProductMessage>(subject, QueueGroup, stoppingToken),
                options,
                async (msg, token) => await HandleRequestAsync(subject, msg, token));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{Subject} request handling cancelled", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {Subject} requests", subject);
        }
    }

    private async Task ReplyAsync(string replyTo, BaseResponse response, CancellationToken cancellationToken)
    {
        try
        {
            // Publish with the concrete type: the JSON serializer only writes the members of the declared type
            switch (response)
            {
                case ProductListResponse list:
                    await _natsService.PublishAsync(replyTo, list, cancellationToken);
                    break;
                case ProductResponse single:
                    await _natsService.PublishAsync(replyTo, single, cancellationToken);
                    break;
                default:
                    await _natsService.PublishAsync(replyTo, response, cancellationToken);
                    break;
            }

            _logger.LogDebug("Sent response to {ReplyTo}", replyTo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending response to {ReplyTo}", replyTo);
        }
    }

    private async Task PublishChangeAsync(ProductChangedEvent change, CancellationToken cancellationToken)
    {
        try
        {
            await _natsService.PublishAsync(Subjects.ProductsChanged, change, cancellationToken);
        }
        catch (Exception ex)
        {
            // The write already succeeded; caches fall back to their TTL if this event is lost
            _logger.LogError(ex, "Error publishing {ChangeType} event for product {ProductId}",
                change.ChangeType, change.ProductId);
        }
    }

    /// <summary>
    /// Waits for the NATS connection to be established before proceeding
    /// </summary>
    private async Task WaitForNatsConnectionAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 60;
        const int retryDelaySeconds = 10;
        var retryCount = 0;

        while (!_natsService.IsConnected && retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Waiting for NATS connection to be established (attempt {RetryCount}/{MaxRetries})...",
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
}
