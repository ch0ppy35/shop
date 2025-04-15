using Common.Models;

namespace Cart.Services;

/// <summary>
/// Service for cart operations
/// </summary>
public class CartService
{
    private readonly ILogger<CartService> _logger;
    private readonly RedisService _redisService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartService"/> class.
    /// </summary>
    public CartService(ILogger<CartService> logger, RedisService redisService)
    {
        _logger = logger;
        _redisService = redisService;
    }

    /// <summary>
    /// Gets a cart by session ID
    /// </summary>
    public async Task<CartResponse> GetCartAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting cart for session ID: {SessionId}", sessionId);

        try
        {
            var cartKey = GetCartKey(sessionId);
            var cartItems = await _redisService.GetAsync<List<CartItem>>(cartKey, cancellationToken) ?? new List<CartItem>();

            var totalPrice = cartItems.Sum(item => item.TotalPrice);
            var itemCount = cartItems.Sum(item => item.Quantity);

            var response = new CartResponse
            {
                Success = true,
                Message = "Cart retrieved successfully",
                SessionId = sessionId,
                Items = cartItems,
                TotalPrice = totalPrice,
                ItemCount = itemCount
            };

            _logger.LogInformation("Cart retrieved for session ID: {SessionId}, Items: {ItemCount}, Total: {TotalPrice}",
                sessionId, itemCount, totalPrice);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for session ID: {SessionId}", sessionId);
            return new CartResponse
            {
                Success = false,
                Error = $"Error getting cart: {ex.Message}",
                SessionId = sessionId,
                Items = new List<CartItem>(),
                TotalPrice = 0,
                ItemCount = 0
            };
        }
    }

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    public async Task<CartResponse> AddItemAsync(string sessionId, CartItem item, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding item to cart for session ID: {SessionId}, Product ID: {ProductId}, Quantity: {Quantity}",
            sessionId, item.ProductId, item.Quantity);

        try
        {
            var cartKey = GetCartKey(sessionId);
            var cartItems = await _redisService.GetAsync<List<CartItem>>(cartKey, cancellationToken) ?? new List<CartItem>();

            var existingItem = cartItems.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
                _logger.LogInformation("Updated existing item in cart, new quantity: {Quantity}", existingItem.Quantity);
            }
            else
            {
                cartItems.Add(item);
                _logger.LogInformation("Added new item to cart");
            }

            await _redisService.SetAsync(cartKey, cartItems, _redisService.CartTtl, cancellationToken);

            var totalPrice = cartItems.Sum(i => i.TotalPrice);
            var itemCount = cartItems.Sum(i => i.Quantity);

            var response = new CartResponse
            {
                Success = true,
                Message = "Item added to cart successfully",
                SessionId = sessionId,
                Items = cartItems,
                TotalPrice = totalPrice,
                ItemCount = itemCount
            };

            _logger.LogInformation("Item added to cart for session ID: {SessionId}, Items: {ItemCount}, Total: {TotalPrice}",
                sessionId, itemCount, totalPrice);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for session ID: {SessionId}", sessionId);
            return new CartResponse
            {
                Success = false,
                Error = $"Error adding item to cart: {ex.Message}",
                SessionId = sessionId
            };
        }
    }

    /// <summary>
    /// Updates an item in the cart
    /// </summary>
    public async Task<CartResponse> UpdateItemAsync(string sessionId, string productId, int quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating item in cart for session ID: {SessionId}, Product ID: {ProductId}, Quantity: {Quantity}",
            sessionId, productId, quantity);

        try
        {
            var cartKey = GetCartKey(sessionId);
            var cartItems = await _redisService.GetAsync<List<CartItem>>(cartKey, cancellationToken) ?? new List<CartItem>();

            var existingItem = cartItems.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem == null)
            {
                _logger.LogWarning("Item not found in cart: {ProductId}", productId);
                return new CartResponse
                {
                    Success = false,
                    Error = $"Item with ID {productId} not found in cart",
                    SessionId = sessionId,
                    Items = cartItems,
                    TotalPrice = cartItems.Sum(i => i.TotalPrice),
                    ItemCount = cartItems.Sum(i => i.Quantity)
                };
            }

            if (quantity <= 0)
            {
                cartItems.Remove(existingItem);
                _logger.LogInformation("Removed item from cart: {ProductId}", productId);
            }
            else
            {
                existingItem.Quantity = quantity;
                _logger.LogInformation("Updated item quantity in cart: {ProductId}, Quantity: {Quantity}", productId, quantity);
            }

            await _redisService.SetAsync(cartKey, cartItems, _redisService.CartTtl, cancellationToken);

            var totalPrice = cartItems.Sum(i => i.TotalPrice);
            var itemCount = cartItems.Sum(i => i.Quantity);

            var response = new CartResponse
            {
                Success = true,
                Message = "Cart updated successfully",
                SessionId = sessionId,
                Items = cartItems,
                TotalPrice = totalPrice,
                ItemCount = itemCount
            };

            _logger.LogInformation("Item updated in cart for session ID: {SessionId}, Items: {ItemCount}, Total: {TotalPrice}",
                sessionId, itemCount, totalPrice);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item in cart for session ID: {SessionId}", sessionId);
            return new CartResponse
            {
                Success = false,
                Error = $"Error updating item in cart: {ex.Message}",
                SessionId = sessionId
            };
        }
    }

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    public async Task<CartResponse> RemoveItemAsync(string sessionId, string productId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing item from cart for session ID: {SessionId}, Product ID: {ProductId}",
            sessionId, productId);

        try
        {
            var cartKey = GetCartKey(sessionId);
            var cartItems = await _redisService.GetAsync<List<CartItem>>(cartKey, cancellationToken) ?? new List<CartItem>();

            var existingItem = cartItems.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem == null)
            {
                _logger.LogWarning("Item not found in cart: {ProductId}", productId);
                return new CartResponse
                {
                    Success = false,
                    Error = $"Item with ID {productId} not found in cart",
                    SessionId = sessionId,
                    Items = cartItems,
                    TotalPrice = cartItems.Sum(i => i.TotalPrice),
                    ItemCount = cartItems.Sum(i => i.Quantity)
                };
            }

            cartItems.Remove(existingItem);
            _logger.LogInformation("Removed item from cart: {ProductId}", productId);

            await _redisService.SetAsync(cartKey, cartItems, _redisService.CartTtl, cancellationToken);

            var totalPrice = cartItems.Sum(i => i.TotalPrice);
            var itemCount = cartItems.Sum(i => i.Quantity);

            var response = new CartResponse
            {
                Success = true,
                Message = "Item removed from cart successfully",
                SessionId = sessionId,
                Items = cartItems,
                TotalPrice = totalPrice,
                ItemCount = itemCount
            };

            _logger.LogInformation("Item removed from cart for session ID: {SessionId}, Items: {ItemCount}, Total: {TotalPrice}",
                sessionId, itemCount, totalPrice);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart for session ID: {SessionId}", sessionId);
            return new CartResponse
            {
                Success = false,
                Error = $"Error removing item from cart: {ex.Message}",
                SessionId = sessionId
            };
        }
    }

    /// <summary>
    /// Clears the cart
    /// </summary>
    public async Task<CartResponse> ClearCartAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing cart for session ID: {SessionId}", sessionId);

        try
        {
            var cartKey = GetCartKey(sessionId);
            await _redisService.RemoveAsync(cartKey, cancellationToken);

            var response = new CartResponse
            {
                Success = true,
                Message = "Cart cleared successfully",
                SessionId = sessionId,
                Items = new List<CartItem>(),
                TotalPrice = 0,
                ItemCount = 0
            };

            _logger.LogInformation("Cart cleared for session ID: {SessionId}", sessionId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for session ID: {SessionId}", sessionId);
            return new CartResponse
            {
                Success = false,
                Error = $"Error clearing cart: {ex.Message}",
                SessionId = sessionId
            };
        }
    }

    /// <summary>
    /// Gets the Redis key for a cart
    /// </summary>
    private static string GetCartKey(string sessionId) => $"cart:{sessionId}";
}
