using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace Gateway.Services;

/// <summary>
/// Read-through cache for catalog reads: in-process memory (L1) backed by Valkey (L2) when one is configured
/// </summary>
/// <remarks>
/// A hit skips NATS and Postgres entirely. Entries are dropped by tag when <see cref="Subjects.ProductsChanged"/>
/// arrives (see <see cref="GatewayEventService"/>); the expiration is only a safety net for a lost event.
/// Only successful responses are cached, and never with a session ID, since every shopper shares them.
/// </remarks>
public class CatalogCache
{
    /// <summary>
    /// Tag on every entry that depends on the catalog as a whole (pages, recommendations)
    /// </summary>
    public const string CatalogTag = "catalog";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    private readonly HybridCache _cache;
    private readonly INatsService _natsService;
    private readonly ILogger<CatalogCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogCache"/> class.
    /// </summary>
    public CatalogCache(HybridCache cache, INatsService natsService, ILogger<CatalogCache> logger)
    {
        _cache = cache;
        _natsService = natsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the tag on a single product's entry
    /// </summary>
    public static string ProductTag(string productId) => $"product:{productId}";

    /// <summary>
    /// Gets one product
    /// </summary>
    public Task<ProductResponse?> GetProductAsync(string productId, CancellationToken cancellationToken = default) =>
        GetOrFetchAsync($"products:get:{productId}", new[] { ProductTag(productId) },
            ct => _natsService.RequestAsync<ProductMessage, ProductResponse>("products.get",
                new ProductMessage { ProductId = productId, OperationType = ProductOperationType.Get },
                RequestTimeout, ct),
            cancellationToken);

    /// <summary>
    /// Gets one page of products
    /// </summary>
    public Task<ProductListResponse?> GetPageAsync(int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        GetOrFetchAsync($"products:page:{page}:{pageSize}", new[] { CatalogTag },
            ct => _natsService.RequestAsync<ProductMessage, ProductListResponse>("products.getall",
                new ProductMessage { OperationType = ProductOperationType.GetAll, PageNumber = page, PageSize = pageSize },
                RequestTimeout, ct),
            cancellationToken);

    /// <summary>
    /// Gets recommendations for a set of products. The result depends only on the IDs and the catalog,
    /// so it is shared by every cart holding the same products.
    /// </summary>
    public Task<RecommendationResponse?> GetRecommendationsAsync(IReadOnlyCollection<string> productIds, int max,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().Order(StringComparer.Ordinal).ToList();

        return GetOrFetchAsync($"recs:{max}:{string.Join(',', ids)}", new[] { CatalogTag },
            ct => _natsService.RequestAsync<RecommendationMessage, RecommendationResponse>("recommendations.get",
                new RecommendationMessage
                {
                    OperationType = RecommendationOperationType.GetRecommendations,
                    CartItems = ids.Select(id => new CartItem { ProductId = id, Quantity = 1 }).ToList(),
                    MaxRecommendations = max
                },
                RequestTimeout, ct),
            cancellationToken);
    }

    /// <summary>
    /// Drops every entry a product change can affect
    /// </summary>
    public async Task InvalidateAsync(ProductChangedEvent change, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByTagAsync(new[] { CatalogTag, ProductTag(change.ProductId) }, cancellationToken);
        _logger.LogInformation("Invalidated catalog cache for {ChangeType} of product {ProductId}",
            change.ChangeType, change.ProductId);
    }

    private async Task<T?> GetOrFetchAsync<T>(string key, IEnumerable<string> tags,
        Func<CancellationToken, Task<T?>> fetch, CancellationToken cancellationToken)
        where T : BaseResponse
    {
        try
        {
            return await _cache.GetOrCreateAsync(key, fetch, static async (fetch, ct) =>
                {
                    var response = await fetch(ct);
                    if (response is not { Success: true })
                    {
                        // Throwing keeps HybridCache from storing it; failures and not-found answers aren't cached
                        throw new UncacheableResponseException(response);
                    }

                    response.SessionId = null;
                    return response;
                },
                EntryOptions, tags, cancellationToken);
        }
        catch (UncacheableResponseException ex)
        {
            return (T?)ex.Response;
        }
    }

    private sealed class UncacheableResponseException(BaseResponse? response) : Exception
    {
        public BaseResponse? Response { get; } = response;
    }
}
