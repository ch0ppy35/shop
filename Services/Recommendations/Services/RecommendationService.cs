using Common.Messaging;
using Common.Models;

namespace Recommendations.Services;

/// <summary>
/// Service for generating product recommendations
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly ILogger<RecommendationService> _logger;
    private readonly INatsService _natsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationService"/> class.
    /// </summary>
    public RecommendationService(ILogger<RecommendationService> logger, INatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    /// <summary>
    /// Gets recommendations based on cart items
    /// </summary>
    public async Task<List<ProductMessage>> GetRecommendationsAsync(
        string sessionId,
        List<CartItem> cartItems,
        int maxRecommendations = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating recommendations for session ID: {SessionId}, Items: {ItemCount}",
            sessionId, cartItems.Count);

        if (cartItems.Count == 0)
        {
            _logger.LogInformation("No items in cart, returning popular products");
            return await GetPopularProductsAsync(sessionId, maxRecommendations, cancellationToken);
        }

        // Get all products to generate recommendations from
        var allProducts = await GetAllProductsAsync(sessionId, cancellationToken);
        if (allProducts.Count == 0)
        {
            _logger.LogWarning("No products available for recommendations");
            return new List<ProductMessage>();
        }

        // Get product IDs from cart
        var cartProductIds = cartItems.Select(i => i.ProductId).ToHashSet();

        // Filter out products already in cart
        var availableProducts = allProducts
            .Where(p => !cartProductIds.Contains(p.ProductId))
            .ToList();

        if (availableProducts.Count == 0)
        {
            _logger.LogInformation("No available products for recommendations (all products are in cart)");
            return new List<ProductMessage>();
        }

        // Simple recommendation algorithm:
        // 1. For each product in cart, find products in similar price range
        // 2. Sort by price similarity to cart items
        // 3. Take top N recommendations

        var recommendations = new List<(ProductMessage Product, decimal Score)>();

        // Calculate average price of cart items
        var avgCartPrice = cartItems.Average(i => i.Price);

        // Score each available product based on price similarity to cart items
        foreach (var product in availableProducts)
        {
            // Calculate price similarity score (lower difference = higher score)
            var priceDifference = Math.Abs(product.Price - avgCartPrice);
            var similarityScore = 1.0m / (1.0m + priceDifference);

            recommendations.Add((product, similarityScore));
        }

        // Sort by score (descending) and take top N
        var topRecommendations = recommendations
            .OrderByDescending(r => r.Score)
            .Take(maxRecommendations)
            .Select(r => r.Product)
            .ToList();

        _logger.LogInformation("Generated {Count} recommendations for session ID: {SessionId}",
            topRecommendations.Count, sessionId);

        return topRecommendations;
    }

    /// <summary>
    /// Gets popular products when cart is empty
    /// </summary>
    private async Task<List<ProductMessage>> GetPopularProductsAsync(
        string sessionId,
        int maxProducts,
        CancellationToken cancellationToken)
    {
        // For now, just return some products
        // In a real implementation, this would use analytics data to determine popular products
        var allProducts = await GetAllProductsAsync(sessionId, cancellationToken);

        // Sort by price (as a simple proxy for popularity)
        return allProducts
            .OrderBy(p => p.Price)
            .Take(maxProducts)
            .ToList();
    }

    /// <summary>
    /// Gets all products from the product service
    /// </summary>
    private async Task<List<ProductMessage>> GetAllProductsAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create request message
            var message = new ProductMessage
            {
                OperationType = ProductOperationType.GetAll,
                SessionId = sessionId,
                PageNumber = 1,
                PageSize = 100 // Get a large number of products
            };

            // Send request to products service
            var response = await _natsService.RequestAsync<ProductMessage, ProductListResponse>(
                "products.getall",
                message,
                TimeSpan.FromSeconds(5),
                cancellationToken);

            if (response == null || !response.Success || response.Products == null)
            {
                _logger.LogWarning("Failed to get products for recommendations");
                return new List<ProductMessage>();
            }

            return response.Products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for recommendations");
            return new List<ProductMessage>();
        }
    }
}
