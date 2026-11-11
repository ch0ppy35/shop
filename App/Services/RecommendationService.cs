using System.Net.Http.Json;
using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Service for interacting with the recommendations API
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationService"/> class
    /// </summary>
    public RecommendationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Sends the product IDs the browser already holds, so the gateway needs no cart lookup and can serve the
    /// answer from its cache. The request carries no session header, so it needs no CORS preflight.
    /// </remarks>
    public async Task<List<Product>> GetRecommendationsAsync(IEnumerable<string> productIds,
        int maxRecommendations = 5)
    {
        try
        {
            var ids = string.Join(',', productIds.Distinct().Order(StringComparer.Ordinal));
            var response =
                await _httpClient.GetFromJsonAsync<RecommendationResponse>(
                    $"/api/recommendations?productIds={Uri.EscapeDataString(ids)}&maxRecommendations={maxRecommendations}");

            if (response == null || response.Recommendations == null)
            {
                return new List<Product>();
            }

            return response.Recommendations.Select(p => new Product
            {
                Id = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Sku = p.Sku,
                Location = p.Location,
                QuantityInStock = p.QuantityInStock
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting recommendations: {ex.Message}");
            return new List<Product>();
        }
    }

    private class RecommendationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public List<ProductDto>? Recommendations { get; set; }
    }

    private class ProductDto
    {
        public string? ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Sku { get; set; }
        public string? Location { get; set; }
        public int QuantityInStock { get; set; }
    }
}