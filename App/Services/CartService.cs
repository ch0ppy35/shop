using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Service for interacting with the cart API
/// </summary>
public class CartService : ICartService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartService"/> class
    /// </summary>
    public CartService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <summary>
    /// Gets the current cart
    /// </summary>
    public async Task<ShoppingCart> GetCartAsync()
    {
        try
        {
            // Use the injected HttpClient which already has the session ID header
            var response = await _httpClient.GetFromJsonAsync<CartResponse>("/api/cart");

            if (response == null || response.Items == null)
            {
                return new ShoppingCart
                {
                    Items = new List<CartItem>(),
                    TotalPrice = 0,
                    ItemCount = 0
                };
            }

            return new ShoppingCart
            {
                Items = response.Items.Select(i => new CartItem
                {
                    ProductId = i.ProductId,
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList(),
                TotalPrice = response.TotalPrice,
                ItemCount = response.ItemCount
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting cart: {ex.Message}");
            return new ShoppingCart
            {
                Items = new List<CartItem>(),
                TotalPrice = 0,
                ItemCount = 0
            };
        }
    }

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="quantity">The quantity to add</param>
    public async Task<bool> AddItemAsync(string productId, int quantity = 1)
    {
        try
        {
            if (string.IsNullOrEmpty(productId) || quantity <= 0)
            {
                return false;
            }

            var cartItemDto = new CartItemDto
            {
                ProductId = productId,
                Quantity = quantity
            };

            var content = new StringContent(
                JsonSerializer.Serialize(cartItemDto, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/cart/items", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding item to cart: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates an item in the cart
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="quantity">The new quantity</param>
    public async Task<bool> UpdateItemAsync(string productId, int quantity)
    {
        try
        {
            if (string.IsNullOrEmpty(productId) || quantity < 0)
            {
                return false;
            }

            var updateDto = new CartItemUpdateDto
            {
                Quantity = quantity
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync($"/api/cart/items/{productId}", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating item in cart: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    /// <param name="productId">The product ID</param>
    public async Task<bool> RemoveItemAsync(string productId)
    {
        try
        {
            if (string.IsNullOrEmpty(productId))
            {
                return false;
            }

            var response = await _httpClient.DeleteAsync($"/api/cart/items/{productId}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing item from cart: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clears the cart
    /// </summary>
    public async Task<bool> ClearCartAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/api/cart");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing cart: {ex.Message}");
            return false;
        }
    }

    // Response classes to match the API
    private class CartResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public List<CartItemDto>? Items { get; set; }
        public decimal TotalPrice { get; set; }
        public int ItemCount { get; set; }
    }

    private class CartItemDto
    {
        public string? ProductId { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }

    private class CartItemUpdateDto
    {
        public int Quantity { get; set; }
    }
}
