using Common.Messaging;
using Common.Models;
using Gateway.Filters;
using Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

/// <summary>
/// Controller for recommendation operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private const int MaxProductIds = 50;

    private readonly ILogger<RecommendationsController> _logger;
    private readonly INatsService _natsService;
    private readonly CatalogCache _catalogCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationsController"/> class.
    /// </summary>
    public RecommendationsController(ILogger<RecommendationsController> logger, INatsService natsService,
        CatalogCache catalogCache)
    {
        _logger = logger;
        _natsService = natsService;
        _catalogCache = catalogCache;
    }

    /// <summary>
    /// Gets recommendations for a set of products, such as the cart the browser already holds
    /// </summary>
    /// <param name="productIds">Comma-separated product IDs; empty for "popular" picks</param>
    /// <param name="maxRecommendations">How many to return</param>
    /// <remarks>
    /// Needs no cart lookup and depends only on the IDs and the catalog, so results are cached and shared.
    /// </remarks>
    [HttpGet]
    [ETag]
    public async Task<IActionResult> GetRecommendations([FromQuery] string? productIds,
        [FromQuery] int maxRecommendations = 5)
    {
        var ids = (productIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (ids.Count > MaxProductIds)
        {
            return BadRequest(new { error = $"At most {MaxProductIds} product IDs are allowed" });
        }

        return await RecommendAsync(ids, maxRecommendations);
    }

    /// <summary>
    /// Gets recommendations based on the session's cart
    /// </summary>
    /// <remarks>
    /// Kept for older clients. It costs an extra cart lookup; use <see cref="GetRecommendations"/> when the
    /// caller already knows what is in the cart.
    /// </remarks>
    [HttpGet("cart")]
    public async Task<IActionResult> GetCartRecommendations([FromQuery] int maxRecommendations = 5)
    {
        try
        {
            var sessionId = HttpContext.Items["SessionId"]?.ToString();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var cartResponse = await _natsService.RequestAsync<CartMessage, CartResponse>(
                "cart.get",
                new CartMessage { OperationType = CartOperationType.GetCart, SessionId = sessionId },
                TimeSpan.FromSeconds(5));

            if (cartResponse is not { Success: true })
            {
                return StatusCode(400, new { error = cartResponse?.Error ?? "Failed to get cart" });
            }

            var ids = (cartResponse.Items ?? new List<CartItem>())
                .Select(i => i.ProductId)
                .OfType<string>()
                .ToList();

            return await RecommendAsync(ids, maxRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart recommendations");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    private async Task<IActionResult> RecommendAsync(IReadOnlyCollection<string> productIds, int maxRecommendations)
    {
        maxRecommendations = Math.Clamp(maxRecommendations, 1, 20);

        try
        {
            var response = await _catalogCache.GetRecommendationsAsync(productIds, maxRecommendations,
                HttpContext.RequestAborted);

            if (response == null)
            {
                return StatusCode(500, new { error = "No response received from recommendation service" });
            }

            if (!response.Success)
            {
                return StatusCode(400, new { error = response.Error ?? "Failed to get recommendations" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }
}
