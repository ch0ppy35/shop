using Common.Messaging;
using Common.Models;

namespace Recommendations.Services;

/// <summary>
/// Background service for handling recommendation requests
/// </summary>
public class RecommendationConsumerService : BackgroundService
{
    private readonly ILogger<RecommendationConsumerService> _logger;
    private readonly INatsService _natsService;
    private readonly IRecommendationService _recommendationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationConsumerService"/> class.
    /// </summary>
    public RecommendationConsumerService(
        ILogger<RecommendationConsumerService> logger,
        INatsService natsService,
        IRecommendationService recommendationService)
    {
        _logger = logger;
        _natsService = natsService;
        _recommendationService = recommendationService;
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recommendation consumer service starting");

        await WaitForNatsConnectionAsync(stoppingToken);

        var tasks = new List<Task>
        {
            HandleGetRecommendationsRequests(stoppingToken)
        };

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Waits for NATS connection to be established
    /// </summary>
    private async Task WaitForNatsConnectionAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 30;
        var retryCount = 0;

        while (!_natsService.IsConnected && !stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            _logger.LogInformation(
                "Waiting for NATS connection to be established... (Attempt {RetryCount}/{MaxRetries})",
                retryCount + 1, maxRetries);

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
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

    /// <summary>
    /// Handles requests for recommendations
    /// </summary>
    private async Task HandleGetRecommendationsRequests(CancellationToken stoppingToken)
    {
        const string subject = "recommendations.get";
        const string queueGroup = "recommendations-service";
        _logger.LogInformation("Starting to handle requests from subject: {Subject} with queue group: {QueueGroup}",
            subject, queueGroup);

        try
        {
            await foreach (var msg in _natsService.SubscribeAsync<RecommendationMessage>(subject, queueGroup,
                               stoppingToken))
            {
                _logger.LogInformation(
                    "Received get recommendations request - SessionId: {SessionId}, CartItems: {CartItemCount}",
                    msg.SessionId ?? "Unknown", msg.CartItems?.Count ?? 0);

                if (string.IsNullOrEmpty(msg.SessionId))
                {
                    _logger.LogWarning("Get recommendations request missing session ID");
                    continue;
                }

                var response = new RecommendationResponse { Success = false, SessionId = msg.SessionId };

                try
                {
                    var cartItems = msg.CartItems ?? new List<CartItem>();
                    var maxRecommendations = msg.MaxRecommendations > 0 ? msg.MaxRecommendations : 5;

                    var recommendations = await _recommendationService.GetRecommendationsAsync(
                        msg.SessionId,
                        cartItems,
                        maxRecommendations,
                        stoppingToken);

                    response.Success = true;
                    response.Message = "Recommendations generated successfully";
                    response.Recommendations = recommendations;
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
            _logger.LogInformation("Get recommendations request handling cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling get recommendations requests");
        }
    }
}