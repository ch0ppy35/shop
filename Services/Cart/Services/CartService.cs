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
            // Get the cart from Redis
            var cartKey = GetCartKey(sessionId);
            var cartItems = await _redisService.GetAsync<List<CartItem>>(cartKey, cancellationToken) ?? new List<CartItem>();

            // Calculate totals
            var totalPrice = cartItems.Sum(item => item.TotalPrice);
            var itemCount = cartItems.Sum(item => item.Quantity);

            // Create the response
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
            // Get the cart from Redis
            var cartKey = GetCartKey(sessionId);
            var cartItems = await _redisService.GetAsync<List<CartItem>>(cartKey, cancellationToken) ?? new List<CartItem>();

            // Check if the item already exists in the cart
            var existingItem = cartItems.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existingItem != null)
            {
                // Update the quantity
                existingItem.Quantity += item.Quantity;
                _logger.LogInformation("Updated existing item in cart, new quantity: {Quantity}", existingItem.Quantity);
            }
            else
            {
                // Add the new item
                cartItems.Add(item);
                _logger.LogInformation("Added new item to cart");
            }

            // Save the cart to Redis with TTL
            await _redisService.SetAsync(cartKey, cartItems, _redisService.CartTtl, cancellationToken);

            // Calculate totals
            var totalPrice = cartItems.Sum(i => i.TotalPrice);
            var itemCount = cartItems.Sum(i => i.Quantity);

            // Create the response
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
            // Get the cart from Redis
            var cartKey = GetCartKey(sessionId);
            var cartItems = await _redisService.GetAsync<List<CartItem>>(cartKey, cancellationToken) ?? new List<CartItem>();

            // Find the item in the cart
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

            // Update the quantity
            if (quantity <= 0)
            {
                // Remove the item if quantity is 0 or negative
                cartItems.Remove(existingItem);
                _logger.LogInformation("Removed item from cart: {ProductId}", productId);
            }
            else
            {
                // Update the quantity
                existingItem.Quantity = quantity;
                _logger.LogInformation("Updated item quantity in cart: {ProductId}, Quantity: {Quantity}", productId, quantity);
            }

            // Save the cart to Redis with TTL
            await _redisService.SetAsync(cartKey, cartItems, _redisService.CartTtl, cancellationToken);

            // Calculate totals
            var totalPrice = cartItems.Sum(i => i.TotalPrice);
            var itemCount = cartItems.Sum(i => i.Quantity);

            // Create the response
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
            // Get the cart from Redis
            var cartKey = GetCartKey(sessionId);
            var cartItems = await _redisService.GetAsync<List<CartItem>>(cartKey, cancellationToken) ?? new List<CartItem>();

            // Find the item in the cart
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

            // Remove the item
            cartItems.Remove(existingItem);
            _logger.LogInformation("Removed item from cart: {ProductId}", productId);

            // Save the cart to Redis with TTL
            await _redisService.SetAsync(cartKey, cartItems, _redisService.CartTtl, cancellationToken);

            // Calculate totals
            var totalPrice = cartItems.Sum(i => i.TotalPrice);
            var itemCount = cartItems.Sum(i => i.Quantity);

            // Create the response
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
            // Remove the cart from Redis
            var cartKey = GetCartKey(sessionId);
            await _redisService.RemoveAsync(cartKey, cancellationToken);

            // Create the response
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
