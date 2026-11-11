using Common.Models;

namespace Recommendations.Services;

/// <summary>
/// Service for generating product recommendations
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly ILogger<RecommendationService> _logger;
    private readonly IProductCatalog _catalog;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationService"/> class.
    /// </summary>
    public RecommendationService(ILogger<RecommendationService> logger, IProductCatalog catalog)
    {
        _logger = logger;
        _catalog = catalog;
    }

    /// <summary>
    /// Gets recommendations based on cart items
    /// </summary>
    /// <remarks>
    /// Cart items only need a product ID; a missing price is looked up in the catalog.
    /// </remarks>
    public async Task<List<ProductMessage>> GetRecommendationsAsync(
        string sessionId,
        List<CartItem> cartItems,
        int maxRecommendations = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating recommendations for session ID: {SessionId}, Items: {ItemCount}",
            sessionId, cartItems.Count);

        var allProducts = await _catalog.GetProductsAsync(sessionId, cancellationToken);
        if (allProducts.Count == 0)
        {
            _logger.LogWarning("No products available for recommendations");
            return new List<ProductMessage>();
        }

        if (cartItems.Count == 0)
        {
            return allProducts
                .OrderBy(p => p.Price)
                .Take(maxRecommendations)
                .ToList();
        }

        var byId = allProducts.Where(p => p.ProductId != null).ToDictionary(p => p.ProductId!);
        var cartPrices = cartItems
            .Select(i => i.Price > 0 ? i.Price : byId.GetValueOrDefault(i.ProductId ?? "")?.Price)
            .OfType<decimal>()
            .ToList();
        var avgCartPrice = cartPrices.Count > 0 ? cartPrices.Average() : 0m;
        var cartProductIds = cartItems.Select(i => i.ProductId).ToHashSet();

        var recommendations = allProducts
            .Where(p => !cartProductIds.Contains(p.ProductId))
            .OrderByDescending(p => 1.0m / (1.0m + Math.Abs(p.Price - avgCartPrice)))
            .ThenBy(p => p.ProductId)
            .Take(maxRecommendations)
            .ToList();

        _logger.LogInformation("Generated {Count} recommendations for session ID: {SessionId}",
            recommendations.Count, sessionId);

        return recommendations;
    }
}
