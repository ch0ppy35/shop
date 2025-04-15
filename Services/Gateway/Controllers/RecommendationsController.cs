using Common.Messaging;
using Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

/// <summary>
/// Controller for recommendation operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly ILogger<RecommendationsController> _logger;
    private readonly INatsService _natsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationsController"/> class.
    /// </summary>
    public RecommendationsController(ILogger<RecommendationsController> logger, INatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    /// <summary>
    /// Gets recommendations based on the current cart
    /// </summary>
    [HttpGet("cart")]
    public async Task<IActionResult> GetCartRecommendations([FromQuery] int maxRecommendations = 5)
    {
        _logger.LogInformation("Received request for cart recommendations, max: {MaxRecommendations}", maxRecommendations);

        try
        {
            var sessionId = HttpContext.Items["SessionId"]?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var cartMessage = new CartMessage
            {
                OperationType = CartOperationType.GetCart,
                SessionId = sessionId
            };

            var cartResponse = await _natsService.RequestAsync<CartMessage, CartResponse>(
                "cart.get",
                cartMessage,
                TimeSpan.FromSeconds(5));

            if (cartResponse == null)
            {
                return StatusCode(500, new { error = "No response received from cart service" });
            }

            if (!cartResponse.Success)
            {
                return StatusCode(400, new { error = cartResponse.Error ?? "Failed to get cart" });
            }

            var recommendationMessage = new RecommendationMessage
            {
                OperationType = RecommendationOperationType.GetRecommendations,
                SessionId = sessionId,
                CartItems = cartResponse.Items,
                MaxRecommendations = maxRecommendations
            };

            var recommendationResponse = await _natsService.RequestAsync<RecommendationMessage, RecommendationResponse>(
                "recommendations.get",
                recommendationMessage,
                TimeSpan.FromSeconds(5));

            if (recommendationResponse == null)
            {
                return StatusCode(500, new { error = "No response received from recommendation service" });
            }

            if (!recommendationResponse.Success)
            {
                return StatusCode(400, new { error = recommendationResponse.Error ?? "Failed to get recommendations" });
            }

            return Ok(recommendationResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart recommendations");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }
}
