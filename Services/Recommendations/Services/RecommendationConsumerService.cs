using Common.Messaging;
using Common.Models;

namespace Recommendations.Services;

/// <summary>
/// Background service for handling recommendation requests and keeping the catalog current
/// </summary>
public class RecommendationConsumerService : BackgroundService
{
    private const string QueueGroup = "recommendations-service";
    private const int MaxConcurrency = 32;

    private readonly ILogger<RecommendationConsumerService> _logger;
    private readonly INatsService _natsService;
    private readonly IRecommendationService _recommendationService;
    private readonly IProductCatalog _catalog;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationConsumerService"/> class.
    /// </summary>
    public RecommendationConsumerService(
        ILogger<RecommendationConsumerService> logger,
        INatsService natsService,
        IRecommendationService recommendationService,
        IProductCatalog catalog)
    {
        _logger = logger;
        _natsService = natsService;
        _recommendationService = recommendationService;
        _catalog = catalog;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recommendation consumer service starting");

        await WaitForNatsConnectionAsync(stoppingToken);

        // Subscribe to changes before loading, so nothing published during the load is missed
        var changes = HandleProductChanges(stoppingToken);
        var requests = HandleGetRecommendationsRequests(stoppingToken);
        _ = _catalog.GetProductsAsync(null, stoppingToken);

        await Task.WhenAll(changes, requests);
    }

    /// <summary>
    /// Answers one recommendation request
    /// </summary>
    public async Task<RecommendationResponse> HandleRequestAsync(RecommendationMessage msg,
        CancellationToken cancellationToken = default)
    {
        var response = new RecommendationResponse { Success = false, SessionId = msg.SessionId };

        try
        {
            response.Recommendations = await _recommendationService.GetRecommendationsAsync(
                msg.SessionId ?? "anonymous",
                msg.CartItems ?? new List<CartItem>(),
                msg.MaxRecommendations > 0 ? msg.MaxRecommendations : 5,
                cancellationToken);
            response.Success = true;
            response.Message = "Recommendations generated successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for session ID: {SessionId}", msg.SessionId);
            response.Error = $"Error generating recommendations: {ex.Message}";
        }

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

        return response;
    }

    private async Task HandleGetRecommendationsRequests(CancellationToken stoppingToken)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency, CancellationToken = stoppingToken };

        try
        {
            await Parallel.ForEachAsync(
                _natsService.SubscribeAsync<RecommendationMessage>("recommendations.get", QueueGroup, stoppingToken),
                options,
                async (msg, token) => await HandleRequestAsync(msg, token));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Get recommendations request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling get recommendations requests");
        }
    }

    private async Task HandleProductChanges(CancellationToken stoppingToken)
    {
        try
        {
            // No queue group: every instance keeps its own catalog, so every instance needs every change
            await foreach (var change in _natsService.SubscribeAsync<ProductChangedEvent>(
                               Subjects.ProductsChanged, cancellationToken: stoppingToken))
            {
                _catalog.Apply(change);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Product change handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling product changes");
        }
    }

    private async Task WaitForNatsConnectionAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 30;
        var retryCount = 0;

        while (!_natsService.IsConnected && !stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            _logger.LogInformation("Waiting for NATS connection (attempt {RetryCount}/{MaxRetries})...",
                retryCount + 1, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            retryCount++;
        }

        if (!_natsService.IsConnected)
        {
            _logger.LogError("NATS connection not established; recommendation handlers may fail");
        }
    }
}
