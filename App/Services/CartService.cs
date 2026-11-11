using System.Net.Http.Json;
using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// Service for interacting with the cart API
/// </summary>
public class CartService : ICartService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartService"/> class
    /// </summary>
    public CartService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public Task<ShoppingCart?> GetCartAsync() =>
        SendAsync("getting cart", () => _httpClient.GetAsync("/api/cart"));

    /// <inheritdoc />
    public Task<ShoppingCart?> AddItemAsync(string productId, int quantity = 1)
    {
        if (string.IsNullOrEmpty(productId) || quantity <= 0)
        {
            return Task.FromResult<ShoppingCart?>(null);
        }

        return SendAsync("adding item to cart", () =>
            _httpClient.PostAsJsonAsync("/api/cart/items", new { productId, quantity }));
    }

    /// <inheritdoc />
    public Task<ShoppingCart?> UpdateItemAsync(string productId, int quantity)
    {
        if (string.IsNullOrEmpty(productId) || quantity < 0)
        {
            return Task.FromResult<ShoppingCart?>(null);
        }

        return SendAsync("updating item in cart", () =>
            _httpClient.PutAsJsonAsync($"/api/cart/items/{Uri.EscapeDataString(productId)}", new { quantity }));
    }

    /// <inheritdoc />
    public Task<ShoppingCart?> RemoveItemAsync(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return Task.FromResult<ShoppingCart?>(null);
        }

        return SendAsync("removing item from cart", () =>
            _httpClient.DeleteAsync($"/api/cart/items/{Uri.EscapeDataString(productId)}"));
    }

    /// <inheritdoc />
    public Task<ShoppingCart?> ClearCartAsync() =>
        SendAsync("clearing cart", () => _httpClient.DeleteAsync("/api/cart"));

    /// <summary>
    /// Converts the API's cart shape (also used by live CartChanged pushes) into the UI model
    /// </summary>
    internal static ShoppingCart ToShoppingCart(CartResponse response) => new()
    {
        Items = (response.Items ?? new List<CartItemDto>()).Select(i => new CartItem
        {
            ProductId = i.ProductId,
            Name = i.Name,
            Price = i.Price,
            Quantity = i.Quantity
        }).ToList(),
        TotalPrice = response.TotalPrice,
        ItemCount = response.ItemCount
    };

    private static async Task<ShoppingCart?> SendAsync(string action, Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error {action}: HTTP {(int)response.StatusCode}");
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<CartResponse>();
            return body == null ? null : ToShoppingCart(body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error {action}: {ex.Message}");
            return null;
        }
    }

    internal class CartResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public List<CartItemDto>? Items { get; set; }
        public decimal TotalPrice { get; set; }
        public int ItemCount { get; set; }
    }

    internal class CartItemDto
    {
        public string? ProductId { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
