using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Gets product recommendations
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Gets recommendations for a set of products, such as what is in the cart; empty when there are none
    /// or the service can't be reached
    /// </summary>
    Task<List<Product>> GetRecommendationsAsync(IEnumerable<string> productIds, int maxRecommendations = 5);
}
