using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Interface for recommendation service
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Gets recommendations based on the current cart
    /// </summary>
    /// <param name="maxRecommendations">The maximum number of recommendations to return</param>
    Task<List<Product>> GetCartRecommendationsAsync(int maxRecommendations = 5);
}