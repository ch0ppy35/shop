using Common.Messaging;
using Common.Models;

namespace Cart.Services;

/// <summary>
/// Hosted service for consuming cart-related messages from NATS
/// </summary>
/// <remarks>
/// Each subject handles up to <see cref="MaxConcurrency"/> messages at once. That is safe because every cart write
/// is a single atomic Valkey transaction (see <see cref="RedisCartStore"/>).
/// </remarks>
public class CartConsumerService : BackgroundService
{
    /// <summary>
    /// The queue group every Cart instance joins
    /// </summary>
    public const string QueueGroup = "cart-service";

    /// <summary>
    /// The most messages a single subject handles at the same time
    /// </summary>
    public const int MaxConcurrency = 32;

    /// <summary>
    /// The request subjects this service answers
    /// </summary>
    public static readonly IReadOnlyList<string> RequestSubjects = new[]
    {
        "cart.get", "cart.additem", "cart.updateitem", "cart.removeitem", "cart.clear"
    };

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

        await WaitForNatsConnectionAsync(stoppingToken);

        var tasks = RequestSubjects.Select(subject => ConsumeAsync(subject, stoppingToken)).ToList();

        await Task.WhenAny(tasks);

        if (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("One of the cart message handlers has stopped unexpectedly");
        }
    }

    /// <summary>
    /// Handles one request: runs it, replies, and publishes the new cart after a successful write
    /// </summary>
    public async Task<CartResponse> HandleRequestAsync(string subject, CartMessage msg,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received {Subject} request - SessionId: {SessionId}, ProductId: {ProductId}",
            subject, msg.SessionId ?? "Unknown", msg.ProductId ?? "-");

        var response = await DispatchAsync(subject, msg, cancellationToken);

        if (!string.IsNullOrEmpty(msg.ReplyTo))
        {
            try
            {
                await _natsService.PublishAsync(msg.ReplyTo, response, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending response to {ReplyTo}", msg.ReplyTo);
            }
        }

        if (response.Success && subject != "cart.get" && Guid.TryParse(msg.SessionId, out _))
        {
            await PublishCartChangedAsync(msg.SessionId!, response, cancellationToken);
        }

        return response;
    }

    private Task<CartResponse> DispatchAsync(string subject, CartMessage msg, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(msg.SessionId))
        {
            return Task.FromResult(Invalid(msg, "Session ID is required"));
        }

        var needsProduct = subject is "cart.additem" or "cart.updateitem" or "cart.removeitem";
        if (needsProduct && string.IsNullOrEmpty(msg.ProductId))
        {
            return Task.FromResult(Invalid(msg, "Product ID is required"));
        }

        return subject switch
        {
            "cart.get" => _cartService.GetCartAsync(msg.SessionId, ct),
            "cart.additem" => _cartService.AddItemAsync(msg.SessionId, new CartItem
            {
                ProductId = msg.ProductId,
                Name = msg.Name,
                Price = msg.Price,
                Quantity = msg.Quantity
            }, ct),
            "cart.updateitem" => _cartService.UpdateItemAsync(msg.SessionId, msg.ProductId!, msg.Quantity, ct),
            "cart.removeitem" => _cartService.RemoveItemAsync(msg.SessionId, msg.ProductId!, ct),
            "cart.clear" => _cartService.ClearCartAsync(msg.SessionId, ct),
            _ => Task.FromResult(Invalid(msg, $"Unknown cart subject {subject}"))
        };
    }

    private CartResponse Invalid(CartMessage msg, string error)
    {
        _logger.LogWarning("Rejected cart request: {Error}", error);
        return new CartResponse { Success = false, Error = error, SessionId = msg.SessionId };
    }

    private async Task PublishCartChangedAsync(string sessionId, CartResponse cart, CancellationToken ct)
    {
        try
        {
            await _natsService.PublishAsync(Subjects.CartChanged(sessionId), new CartChangedEvent
            {
                SessionId = sessionId,
                Items = cart.Items ?? new List<CartItem>(),
                TotalPrice = cart.TotalPrice,
                ItemCount = cart.ItemCount
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing cart change for session {SessionId}", sessionId);
        }
    }

    private async Task ConsumeAsync(string subject, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}",
            subject, QueueGroup);

        var options = new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency, CancellationToken = stoppingToken };

        try
        {
            await Parallel.ForEachAsync(
                _natsService.SubscribeAsync<CartMessage>(subject, QueueGroup, stoppingToken),
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
