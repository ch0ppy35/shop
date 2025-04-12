using System.Net.Http.Json;
using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Service for interacting with the product API
/// </summary>
public class ProductService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigurationService _configService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class
    /// </summary>
    public ProductService(HttpClient httpClient, ConfigurationService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
    }

    /// <summary>
    /// Gets a paginated list of products
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The page size</param>
    public async Task<PaginatedList<Product>> GetProductsAsync(int page = 1, int pageSize = 5)
    {
        try
        {
            // Get the API base URL
            var apiBaseUrl = await _configService.GetApiBaseUrlAsync();

            // Create a new HttpClient with the correct base address
            using var client = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

            var response = await client.GetFromJsonAsync<ProductListResponse>($"/api/products?page={page}&pageSize={pageSize}");

            if (response == null || response.Products == null)
            {
                return new PaginatedList<Product>
                {
                    Items = new List<Product>(),
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                };
            }

            return new PaginatedList<Product>
            {
                Items = response.Products.Select(p => new Product
                {
                    Id = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    Sku = p.Sku,
                    Location = p.Location,
                    QuantityInStock = p.QuantityInStock,
                    ReorderThreshold = p.ReorderThreshold
                }).ToList(),
                PageNumber = response.PageNumber,
                PageSize = response.PageSize,
                TotalCount = response.TotalCount,
                TotalPages = response.TotalPages
            };
        }
        catch (Exception)
        {
            // Return an empty list in case of error
            return new PaginatedList<Product>
            {
                Items = new List<Product>(),
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    /// <param name="id">The product ID</param>
    public async Task<Product?> GetProductAsync(string id)
    {
        try
        {
            // Get the API base URL
            var apiBaseUrl = await _configService.GetApiBaseUrlAsync();

            // Create a new HttpClient with the correct base address
            using var client = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

            var response = await client.GetFromJsonAsync<ProductResponse>($"/api/products/{id}");

            if (response == null || response.Product == null)
            {
                return null;
            }

            return new Product
            {
                Id = response.Product.ProductId,
                Name = response.Product.Name,
                Description = response.Product.Description,
                Price = response.Product.Price,
                Quantity = response.Product.Quantity,
                Sku = response.Product.Sku,
                Location = response.Product.Location,
                QuantityInStock = response.Product.QuantityInStock,
                ReorderThreshold = response.Product.ReorderThreshold
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    // Response classes to match the API
    private class BaseResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    private class ProductResponse : BaseResponse
    {
        public ProductDto? Product { get; set; }
    }

    private class ProductListResponse : BaseResponse
    {
        public List<ProductDto>? Products { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    private class ProductDto
    {
        public string? ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Sku { get; set; }
        public string? Location { get; set; }
        public int QuantityInStock { get; set; }
        public int ReorderThreshold { get; set; }
    }
}
