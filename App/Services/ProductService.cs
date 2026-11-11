using System.Net;
using System.Net.Http.Json;
using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Thrown when the shop's API can't be reached or answers with a server error
/// </summary>
public class ShopUnavailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShopUnavailableException"/> class
    /// </summary>
    public ShopUnavailableException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

/// <summary>
/// A product change pushed by the gateway
/// </summary>
/// <param name="ProductId">The product that changed</param>
/// <param name="ChangeType">Created, Updated, InventoryUpdated or Deleted</param>
/// <param name="Product">The product as it is now, or null when it was deleted</param>
public sealed record ProductChange(string ProductId, string ChangeType, Product? Product);

/// <summary>
/// Reads and writes the catalog, keeping what it has read in memory for the rest of the visit
/// </summary>
/// <remarks>
/// Cached pages and products are rendered straight away. They stay current because the gateway pushes every
/// product change (<see cref="ApplyChange"/>), and the cache is dropped after a reconnect, when pushes may have
/// been missed. Products from a list page also seed the single-product cache, so opening one is instant.
/// </remarks>
public class ProductService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<(int Page, int Size), PaginatedList<Product>> _pages = new();
    private readonly Dictionary<string, Product> _products = new();
    private readonly Dictionary<(int Page, int Size), Task<PaginatedList<Product>>> _pagesInFlight = new();
    private readonly Dictionary<string, Task<Product?>> _productsInFlight = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class
    /// </summary>
    public ProductService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Raised when cached catalog data changed: with a product ID when one product changed in place,
    /// or null when pages may have shifted (products added or removed, or the cache was dropped)
    /// </summary>
    public event Action<string?>? CatalogChanged;

    /// <summary>
    /// Gets a cached page without a request, or null
    /// </summary>
    public PaginatedList<Product>? PeekProducts(int page, int pageSize) =>
        _pages.GetValueOrDefault((page, pageSize));

    /// <summary>
    /// Gets a cached product without a request, or null
    /// </summary>
    public Product? PeekProduct(string id) => _products.GetValueOrDefault(id);

    /// <summary>
    /// Gets a page of products, from memory when already loaded, and prefetches the page after it
    /// </summary>
    /// <exception cref="ShopUnavailableException">The API couldn't be reached</exception>
    public async Task<PaginatedList<Product>> GetProductsAsync(int page = 1, int pageSize = 5)
    {
        var result = await LoadPageAsync(page, pageSize);

        if (page < result.TotalPages)
        {
            _ = PrefetchProductsAsync(page + 1, pageSize);
        }

        return result;
    }

    /// <summary>
    /// Gets a product, or null when there is no product with this ID
    /// </summary>
    /// <exception cref="ShopUnavailableException">The API couldn't be reached</exception>
    public Task<Product?> GetProductAsync(string id)
    {
        if (_products.TryGetValue(id, out var cached))
        {
            return Task.FromResult<Product?>(cached);
        }

        if (!_productsInFlight.TryGetValue(id, out var inFlight))
        {
            inFlight = FetchProductAsync(id);
            if (!inFlight.IsCompleted)
            {
                _productsInFlight[id] = inFlight;
            }
        }

        return inFlight;
    }

    /// <summary>
    /// Starts loading a product in the background, such as when the pointer moves onto it
    /// </summary>
    public void PrefetchProduct(string? id)
    {
        if (!string.IsNullOrEmpty(id) && !_products.ContainsKey(id))
        {
            _ = GetProductAsync(id).ContinueWith(t => _ = t.Exception, TaskScheduler.Default);
        }
    }

    /// <summary>
    /// Applies a pushed product change to everything cached
    /// </summary>
    public void ApplyChange(ProductChange change)
    {
        var changedInPlace = change.Product != null &&
                             change.ChangeType is "Updated" or "InventoryUpdated";

        if (changedInPlace)
        {
            _products[change.ProductId] = change.Product!;
            foreach (var page in _pages.Values)
            {
                var index = page.Items.FindIndex(p => p.Id == change.ProductId);
                if (index >= 0)
                {
                    page.Items[index] = change.Product!;
                }
            }

            CatalogChanged?.Invoke(change.ProductId);
            return;
        }

        // Created or deleted: page boundaries and counts shift, so drop the pages
        _pages.Clear();
        if (change.Product != null)
        {
            _products[change.ProductId] = change.Product;
        }
        else
        {
            _products.Remove(change.ProductId);
        }

        CatalogChanged?.Invoke(null);
    }

    /// <summary>
    /// Drops everything cached, such as after the live connection was lost for a while
    /// </summary>
    public void InvalidateAll()
    {
        _pages.Clear();
        _products.Clear();
        CatalogChanged?.Invoke(null);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    public async Task<Product?> CreateProductAsync(Product product)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/products", ToDto(product));
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
            InvalidateAll();
            return body?.Product == null ? null : ToProduct(body.Product);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    public async Task<bool> UpdateProductAsync(Product product)
    {
        if (string.IsNullOrEmpty(product.Id))
        {
            return false;
        }

        try
        {
            using var response = await _httpClient.PutAsJsonAsync($"/api/products/{product.Id}", ToDto(product));
            if (response.IsSuccessStatusCode)
            {
                InvalidateAll();
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    public async Task<bool> DeleteProductAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        try
        {
            using var response = await _httpClient.DeleteAsync($"/api/products/{id}");
            if (response.IsSuccessStatusCode)
            {
                InvalidateAll();
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts the API's product shape into the UI model
    /// </summary>
    internal static Product ToProduct(ProductDto p) => new()
    {
        Id = p.ProductId,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Sku = p.Sku,
        Location = p.Location,
        QuantityInStock = p.QuantityInStock,
        ReorderThreshold = p.ReorderThreshold
    };

    private async Task PrefetchProductsAsync(int page, int pageSize)
    {
        try
        {
            await LoadPageAsync(page, pageSize);
        }
        catch (ShopUnavailableException)
        {
            // Only a prefetch; the page loads normally when the shopper gets there
        }
    }

    private Task<PaginatedList<Product>> LoadPageAsync(int page, int pageSize)
    {
        var key = (page, pageSize);
        if (_pages.TryGetValue(key, out var cached))
        {
            return Task.FromResult(cached);
        }

        if (!_pagesInFlight.TryGetValue(key, out var inFlight))
        {
            inFlight = FetchPageAsync(page, pageSize);
            if (!inFlight.IsCompleted)
            {
                _pagesInFlight[key] = inFlight;
            }
        }

        return inFlight;
    }

    private async Task<PaginatedList<Product>> FetchPageAsync(int page, int pageSize)
    {
        try
        {
            var response = await GetJsonAsync<ProductListResponse>($"/api/products?page={page}&pageSize={pageSize}");

            var result = new PaginatedList<Product>
            {
                Items = (response?.Products ?? new List<ProductDto>()).Select(ToProduct).ToList(),
                PageNumber = response?.PageNumber ?? page,
                PageSize = response?.PageSize ?? pageSize,
                TotalCount = response?.TotalCount ?? 0,
                TotalPages = response?.TotalPages ?? 0
            };

            _pages[(page, pageSize)] = result;
            foreach (var product in result.Items.Where(p => p.Id != null))
            {
                _products[product.Id!] = product;
            }

            return result;
        }
        finally
        {
            _pagesInFlight.Remove((page, pageSize));
        }
    }

    private async Task<Product?> FetchProductAsync(string id)
    {
        try
        {
            var response = await GetJsonAsync<ProductResponse>($"/api/products/{Uri.EscapeDataString(id)}");
            if (response?.Product == null)
            {
                return null;
            }

            var product = ToProduct(response.Product);
            _products[id] = product;
            return product;
        }
        finally
        {
            _productsInFlight.Remove(id);
        }
    }

    /// <returns>The body, or null for 404</returns>
    private async Task<T?> GetJsonAsync<T>(string uri) where T : class
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(uri);
        }
        catch (Exception ex)
        {
            throw new ShopUnavailableException("The shop couldn't be reached.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ShopUnavailableException($"The shop answered with HTTP {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
    }

    private static object ToDto(Product product) => new
    {
        product.Name,
        product.Description,
        product.Price,
        product.Sku,
        product.Location,
        product.QuantityInStock,
        product.ReorderThreshold
    };

    private class ProductResponse
    {
        public ProductDto? Product { get; set; }
    }

    private class ProductListResponse
    {
        public List<ProductDto>? Products { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// The API's product shape
    /// </summary>
    internal class ProductDto
    {
        public string? ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Sku { get; set; }
        public string? Location { get; set; }
        public int QuantityInStock { get; set; }
        public int ReorderThreshold { get; set; }
    }
}
