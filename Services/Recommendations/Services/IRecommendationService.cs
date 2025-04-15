using Common.Models;

namespace Recommendations.Services;

/// <summary>
/// Interface for recommendation service
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Gets recommendations based on cart items
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="cartItems">The cart items</param>
    /// <param name="maxRecommendations">The maximum number of recommendations to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of recommended products</returns>
    Task<List<ProductMessage>> GetRecommendationsAsync(
        string sessionId, 
        List<CartItem> cartItems, 
        int maxRecommendations = 5,
        CancellationToken cancellationToken = default);
}
