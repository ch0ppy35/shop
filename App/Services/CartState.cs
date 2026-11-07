namespace Frontend.Services;

/// <summary>
/// Shared cart item count so the header badge stays in sync with every page
/// </summary>
public class CartState
{
    private readonly ICartService _cartService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartState"/> class
    /// </summary>
    public CartState(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Raised when the item count changes
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the number of items in the cart
    /// </summary>
    public int ItemCount { get; private set; }

    /// <summary>
    /// Reloads the cart from the API and updates the count
    /// </summary>
    public async Task RefreshAsync()
    {
        var cart = await _cartService.GetCartAsync();
        SetItemCount(cart.ItemCount);
    }

    /// <summary>
    /// Adds a product to the cart and refreshes the count
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="quantity">The quantity to add</param>
    /// <returns>True if the cart accepted the item</returns>
    public async Task<bool> AddAsync(string productId, int quantity = 1)
    {
        var added = await _cartService.AddItemAsync(productId, quantity);
        if (added)
        {
            await RefreshAsync();
        }

        return added;
    }

    /// <summary>
    /// Updates the count from a cart the caller already loaded
    /// </summary>
    /// <param name="itemCount">The number of items in the cart</param>
    public void SetItemCount(int itemCount)
    {
        if (ItemCount == itemCount)
        {
            return;
        }

        ItemCount = itemCount;
        Changed?.Invoke();
    }
}
