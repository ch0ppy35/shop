using System.Collections.Concurrent;
using Common.Messaging;
using Common.Models;

namespace Recommendations.Services;

/// <summary>
/// The product catalog the recommender scores against
/// </summary>
public interface IProductCatalog
{
    /// <summary>
    /// Gets every product, loading the catalog from the Products service the first time
    /// </summary>
    Task<IReadOnlyCollection<ProductMessage>> GetProductsAsync(string? sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a product change so the snapshot stays current without reloading it
    /// </summary>
    void Apply(ProductChangedEvent change);
}

/// <summary>
/// In-memory copy of the catalog, loaded once and then kept current from <see cref="Subjects.ProductsChanged"/>
/// </summary>
/// <remarks>
/// Recommendations used to request the whole products table over NATS on every call. The catalog is small and
/// changes rarely, so holding it in memory turns each recommendation into a pure in-process computation.
/// </remarks>
public class ProductCatalog : IProductCatalog
{
    private const int PageSize = 100;

    private readonly ILogger<ProductCatalog> _logger;
    private readonly INatsService _natsService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly object _swapLock = new();
    private ConcurrentDictionary<string, ProductMessage> _products = new();
    private volatile bool _loaded;
    private long _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductCatalog"/> class.
    /// </summary>
    public ProductCatalog(ILogger<ProductCatalog> logger, INatsService natsService)
    {
        _logger = logger;
        _natsService = natsService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ProductMessage>> GetProductsAsync(string? sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!_loaded)
        {
            await LoadAsync(sessionId, cancellationToken);
        }

        return _products.Values.ToList();
    }

    /// <inheritdoc />
    public void Apply(ProductChangedEvent change)
    {
        lock (_swapLock)
        {
            _version++;

            if (change.ChangeType == ProductChangeType.Deleted || change.Product == null)
            {
                _products.TryRemove(change.ProductId, out _);
            }
            else
            {
                _products[change.ProductId] = change.Product;
            }
        }

        _logger.LogInformation("Applied {ChangeType} for product {ProductId} to the catalog",
            change.ChangeType, change.ProductId);
    }

    private async Task LoadAsync(string? sessionId, CancellationToken cancellationToken)
    {
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_loaded)
            {
                return;
            }

            long versionAtStart;
            lock (_swapLock)
            {
                versionAtStart = _version;
            }

            var loaded = new List<ProductMessage>();
            var page = 1;
            int totalPages;

            do
            {
                var response = await _natsService.RequestAsync<ProductMessage, ProductListResponse>(
                    "products.getall",
                    new ProductMessage
                    {
                        OperationType = ProductOperationType.GetAll,
                        SessionId = sessionId,
                        PageNumber = page,
                        PageSize = PageSize
                    },
                    TimeSpan.FromSeconds(5),
                    cancellationToken);

                if (response is not { Success: true, Products: not null })
                {
                    _logger.LogWarning("Couldn't load page {Page} of the catalog: {Error}", page, response?.Error);
                    return;
                }

                loaded.AddRange(response.Products);
                totalPages = response.TotalPages;
                page++;
            } while (page <= totalPages);

            var snapshot = new ConcurrentDictionary<string, ProductMessage>(
                loaded.Where(p => p.ProductId != null).Select(p => KeyValuePair.Create(p.ProductId!, p)));

            lock (_swapLock)
            {
                // A change that arrived while loading may be newer than what we read, so keep the
                // event-applied data and load again next time
                if (_version != versionAtStart)
                {
                    _logger.LogInformation("Catalog changed while loading; will reload on the next request");
                    return;
                }

                _products = snapshot;
                _loaded = true;
            }

            _logger.LogInformation("Loaded {Count} products into the catalog", snapshot.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error loading the catalog");
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
