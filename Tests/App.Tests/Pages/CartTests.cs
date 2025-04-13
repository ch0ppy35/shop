using Bunit;
using Frontend.Models;
using Frontend.Pages;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace App.Tests.Pages;

public class CartTests : TestContext
{
    private readonly Mock<ICartService> _mockCartService;
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly Mock<ToastService> _mockToastService;
    private readonly Mock<IConfirmService> _mockConfirmService;

    public CartTests()
    {
        _mockCartService = new Mock<ICartService>();
        _mockJsRuntime = new Mock<IJSRuntime>();
        _mockToastService = new Mock<ToastService>();
        _mockConfirmService = new Mock<IConfirmService>();

        // Register the mocked services
        Services.AddSingleton(_mockCartService.Object);
        Services.AddSingleton(_mockJsRuntime.Object);
        Services.AddSingleton(_mockToastService.Object);
        Services.AddSingleton(_mockConfirmService.Object);

        // Setup default JS interop calls
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((object)null!);

        // Setup ConfirmService to return true for confirmations
        _mockConfirmService
            .Setup(cs => cs.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public void Cart_ShouldRenderEmptyCart_WhenCartIsEmpty()
    {
        // Arrange
        var emptyCart = new ShoppingCart
        {
            Items = new List<CartItem>(),
            TotalPrice = 0,
            ItemCount = 0
        };

        _mockCartService
            .Setup(s => s.GetCartAsync())
            .ReturnsAsync(emptyCart);

        // Act
        var cut = RenderComponent<Cart>();

        // Assert
        cut.WaitForState(() => cut.Find("div.alert").TextContent.Contains("Your cart is empty"));
        cut.Find("div.alert").MarkupMatches("<div class=\"alert alert-info\" role=\"alert\">Your cart is empty. <a href=\"/products\" class=\"alert-link\">Browse products</a> to add items to your cart.</div>");
    }

    [Fact]
    public void Cart_ShouldRenderCartItems_WhenCartHasItems()
    {
        // Arrange
        var cart = new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = "prod-1",
                    Name = "Product 1",
                    Price = 10.99m,
                    Quantity = 2
                },
                new CartItem
                {
                    ProductId = "prod-2",
                    Name = "Product 2",
                    Price = 5.99m,
                    Quantity = 1
                }
            },
            TotalPrice = 27.97m,
            ItemCount = 3
        };

        _mockCartService
            .Setup(s => s.GetCartAsync())
            .ReturnsAsync(cart);

        // Act
        var cut = RenderComponent<Cart>();

        // Assert
        cut.WaitForState(() => cut.FindAll("tr").Count > 2); // Header row + 2 item rows

        // Check that we have the correct number of rows (2 items + header + footer)
        var rows = cut.FindAll("tr");
        Assert.Equal(4, rows.Count);

        // Check that the total price is displayed correctly
        var totalCell = cut.Find("tfoot th:nth-child(2)");
        Assert.Contains("$27.97", totalCell.TextContent);

        // Check that the product names are displayed
        Assert.Contains("Product 1", cut.Markup);
        Assert.Contains("Product 2", cut.Markup);

        // Check that the buttons are present
        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count >= 5); // At least 5 buttons (quantity controls, remove, clear cart, continue shopping, checkout)
    }

    [Fact]
    public async Task UpdateQuantity_ShouldCallCartService_AndRefreshCart()
    {
        // Arrange
        var cart = new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = "prod-1",
                    Name = "Product 1",
                    Price = 10.99m,
                    Quantity = 2
                }
            },
            TotalPrice = 21.98m,
            ItemCount = 2
        };

        _mockCartService
            .Setup(s => s.GetCartAsync())
            .ReturnsAsync(cart);

        _mockCartService
            .Setup(s => s.UpdateItemAsync("prod-1", 3))
            .ReturnsAsync(true);

        // Act
        var cut = RenderComponent<Cart>();

        // Find the plus button and click it
        var plusButton = cut.Find("button.btn-primary:nth-child(3)");
        await cut.InvokeAsync(() => plusButton.Click());

        // Assert
        _mockCartService.Verify(s => s.UpdateItemAsync("prod-1", 3), Times.Once);
        _mockCartService.Verify(s => s.GetCartAsync(), Times.Exactly(2)); // Initial load + after update
        _mockJsRuntime.Verify(js => js.InvokeAsync<object>("cartHelper.refreshNavMenu", It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task RemoveItem_ShouldCallCartService_WhenConfirmed()
    {
        // Arrange
        var cart = new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = "prod-1",
                    Name = "Product 1",
                    Price = 10.99m,
                    Quantity = 2
                }
            },
            TotalPrice = 21.98m,
            ItemCount = 2
        };

        _mockCartService
            .Setup(s => s.GetCartAsync())
            .ReturnsAsync(cart);

        _mockCartService
            .Setup(s => s.RemoveItemAsync("prod-1"))
            .ReturnsAsync(true);

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object[]>()))
            .ReturnsAsync(true);

        // Act
        var cut = RenderComponent<Cart>();

        // Find the remove button and click it
        var removeButton = cut.Find("button.btn-danger");
        await cut.InvokeAsync(() => removeButton.Click());

        // Assert
        _mockConfirmService.Verify(cs => cs.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockCartService.Verify(s => s.RemoveItemAsync("prod-1"), Times.Once);
        _mockCartService.Verify(s => s.GetCartAsync(), Times.Exactly(2)); // Initial load + after remove
        _mockJsRuntime.Verify(js => js.InvokeAsync<object>("cartHelper.refreshNavMenu", It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task ClearCart_ShouldCallCartService_WhenConfirmed()
    {
        // Arrange
        var cart = new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = "prod-1",
                    Name = "Product 1",
                    Price = 10.99m,
                    Quantity = 2
                }
            },
            TotalPrice = 21.98m,
            ItemCount = 2
        };

        _mockCartService
            .Setup(s => s.GetCartAsync())
            .ReturnsAsync(cart);

        _mockCartService
            .Setup(s => s.ClearCartAsync())
            .ReturnsAsync(true);

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object[]>()))
            .ReturnsAsync(true);

        // Act
        var cut = RenderComponent<Cart>();

        // Find the clear cart button and click it
        var clearButton = cut.Find("button.btn-outline-danger");
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert
        _mockConfirmService.Verify(cs => cs.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockCartService.Verify(s => s.ClearCartAsync(), Times.Once);
        _mockCartService.Verify(s => s.GetCartAsync(), Times.Exactly(2)); // Initial load + after clear
        _mockJsRuntime.Verify(js => js.InvokeAsync<object>("cartHelper.refreshNavMenu", It.IsAny<object[]>()), Times.Once);
    }
}
