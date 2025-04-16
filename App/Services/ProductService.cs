using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Service for interacting with the product API
/// </summary>
public class ProductService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigurationService _configService;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class
    /// </summary>
    public ProductService(HttpClient httpClient, ConfigurationService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
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
            var response =
                await _httpClient.GetFromJsonAsync<ProductListResponse>(
                    $"/api/products?page={page}&pageSize={pageSize}");

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
            var response = await _httpClient.GetFromJsonAsync<ProductResponse>($"/api/products/{id}");

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

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="product">The product to create</param>
    public async Task<Product?> CreateProductAsync(Product product)
    {
        try
        {
            var productDto = new ProductDto
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Sku = product.Sku,
                Location = product.Location,
                QuantityInStock = product.QuantityInStock,
                ReorderThreshold = product.ReorderThreshold
            };

            var content = new StringContent(
                JsonSerializer.Serialize(productDto, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/products", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseContent = await response.Content.ReadFromJsonAsync<ProductResponse>();

            if (responseContent == null || responseContent.Product == null)
            {
                return null;
            }

            return new Product
            {
                Id = responseContent.Product.ProductId,
                Name = responseContent.Product.Name,
                Description = responseContent.Product.Description,
                Price = responseContent.Product.Price,
                Sku = responseContent.Product.Sku,
                Location = responseContent.Product.Location,
                QuantityInStock = responseContent.Product.QuantityInStock,
                ReorderThreshold = responseContent.Product.ReorderThreshold
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="product">The product to update</param>
    public async Task<bool> UpdateProductAsync(Product product)
    {
        try
        {
            if (string.IsNullOrEmpty(product.Id))
            {
                return false;
            }

            var productDto = new ProductDto
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Sku = product.Sku,
                Location = product.Location,
                QuantityInStock = product.QuantityInStock,
                ReorderThreshold = product.ReorderThreshold
            };

            var content = new StringContent(
                JsonSerializer.Serialize(productDto, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync($"/api/products/{product.Id}", content);

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
    /// <param name="id">The ID of the product to delete</param>
    public async Task<bool> DeleteProductAsync(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            var response = await _httpClient.DeleteAsync($"/api/products/{id}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

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
        public string? Sku { get; set; }
        public string? Location { get; set; }
        public int QuantityInStock { get; set; }
        public int ReorderThreshold { get; set; }
    }
}