using Frontend.Models;

namespace Frontend.Services;

/// <summary>
/// The one copy of the cart every page and the header badge render from
/// </summary>
/// <remarks>
/// <para>Changes are optimistic: the UI updates the moment the shopper clicks, and the request runs behind it.
/// Requests go out one at a time in click order. The cart service handles requests concurrently and quantity
/// updates are absolute ("set to 4"), so overlapping requests could land out of order.</para>
/// <para>Server answers and live <c>CartChanged</c> pushes are applied only when no local change is still in
/// flight, so a stale answer never overwrites a newer click. A failed change falls back to the server's cart.</para>
/// </remarks>
public class CartState
{
    private readonly ICartService _cartService;
    private readonly SemaphoreSlim _writes = new(1, 1);
    private ShoppingCart _confirmed = new();
    private int _pending;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartState"/> class
    /// </summary>
    public CartState(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Raised whenever <see cref="Cart"/> changes
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the cart as the shopper should see it right now, including changes still being sent
    /// </summary>
    public ShoppingCart Cart { get; private set; } = new();

    /// <summary>
    /// Gets the number of items in the cart
    /// </summary>
    public int ItemCount => Cart.ItemCount;

    /// <summary>
    /// Gets whether the cart has been loaded from the server at least once
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Gets whether the last attempt to load the cart failed
    /// </summary>
    public bool IsUnavailable { get; private set; }

    /// <summary>
    /// Gets whether changes are still being sent
    /// </summary>
    public bool IsSaving => _pending > 0;

    /// <summary>
    /// Loads the cart from the server
    /// </summary>
    public async Task RefreshAsync()
    {
        var cart = await _cartService.GetCartAsync();
        if (cart == null)
        {
            IsUnavailable = true;
            Changed?.Invoke();
            return;
        }

        if (_pending == 0)
        {
            Confirm(cart);
        }
    }

    /// <summary>
    /// Loads the cart unless it is already loaded
    /// </summary>
    public Task EnsureLoadedAsync() => IsLoaded ? Task.CompletedTask : RefreshAsync();

    /// <summary>
    /// Applies a cart pushed by the server, such as a change made in another tab
    /// </summary>
    public void ApplyPushed(ShoppingCart cart)
    {
        if (_pending == 0)
        {
            Confirm(cart);
        }
    }

    /// <summary>
    /// Adds a product to the cart
    /// </summary>
    /// <returns>True if the cart accepted the item</returns>
    public Task<bool> AddAsync(Product product, int quantity = 1) =>
        MutateAsync(
            items =>
            {
                var line = items.FirstOrDefault(i => i.ProductId == product.Id);
                if (line != null)
                {
                    line.Quantity += quantity;
                }
                else
                {
                    items.Add(new CartItem
                        { ProductId = product.Id, Name = product.Name, Price = product.Price, Quantity = quantity });
                }
            },
            () => _cartService.AddItemAsync(product.Id!, quantity));

    /// <summary>
    /// Sets the quantity of a line; 0 removes it
    /// </summary>
    public Task<bool> SetQuantityAsync(string productId, int quantity) =>
        MutateAsync(
            items =>
            {
                var line = items.FirstOrDefault(i => i.ProductId == productId);
                if (line == null)
                {
                    return;
                }

                if (quantity <= 0)
                {
                    items.Remove(line);
                }
                else
                {
                    line.Quantity = quantity;
                }
            },
            () => _cartService.UpdateItemAsync(productId, quantity));

    /// <summary>
    /// Removes a line from the cart
    /// </summary>
    public Task<bool> RemoveAsync(string productId) =>
        MutateAsync(
            items => items.RemoveAll(i => i.ProductId == productId),
            () => _cartService.RemoveItemAsync(productId));

    /// <summary>
    /// Empties the cart
    /// </summary>
    public Task<bool> ClearAsync() =>
        MutateAsync(items => items.Clear(), () => _cartService.ClearCartAsync());

    private async Task<bool> MutateAsync(Action<List<CartItem>> change, Func<Task<ShoppingCart?>> send)
    {
        _pending++;
        Show(WithChange(Cart, change));

        ShoppingCart? result;
        await _writes.WaitAsync();
        try
        {
            result = await send();
        }
        finally
        {
            _writes.Release();
            _pending--;
        }

        if (result != null)
        {
            if (_pending == 0)
            {
                Confirm(result);
            }

            return true;
        }

        if (_pending == 0)
        {
            // The server is the source of truth; if it can't be reached either, undo to what it last confirmed
            Confirm(await _cartService.GetCartAsync() ?? _confirmed);
        }

        return false;
    }

    private void Confirm(ShoppingCart cart)
    {
        _confirmed = cart;
        IsLoaded = true;
        IsUnavailable = false;
        Show(cart);
    }

    private void Show(ShoppingCart cart)
    {
        Cart = cart;
        Changed?.Invoke();
    }

    private static ShoppingCart WithChange(ShoppingCart cart, Action<List<CartItem>> change)
    {
        var items = cart.Items.Select(i => new CartItem
            { ProductId = i.ProductId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }).ToList();
        change(items);

        return new ShoppingCart
        {
            Items = items,
            TotalPrice = items.Sum(i => i.TotalPrice),
            ItemCount = items.Sum(i => i.Quantity)
        };
    }
}
