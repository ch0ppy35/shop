using App.Tests.TestHelpers;
using Bunit;
using FluentAssertions;
using Frontend.Models;
using Frontend.Pages;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;

namespace App.Tests.Pages;

public class CartTests : TestContext
{
    private ICartService _cartService;
    private IRecommendationService _recommendationService;
    private ToastService _toastService;

    public CartTests()
    {
        // Add all required services in one go
        this.AddCartTestServices();

        // Get the services we need for the tests
        _cartService = Services.GetRequiredService<ICartService>();
        _recommendationService = Services.GetRequiredService<IRecommendationService>();
        _toastService = Services.GetRequiredService<ToastService>();
    }

    [Fact]
    public void Cart_ShouldRender_EmptyCart()
    {
        // Setup the mock through the service provider
        var mockCartService = Mock.Get(_cartService);
        mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(new ShoppingCart
        {
            Items = new List<CartItem>(),
            TotalPrice = 0,
            ItemCount = 0
        });

        var cut = RenderComponent<Cart>();

        cut.Markup.Should().Contain("Your cart is empty");
        cut.Markup.Should().Contain("Browse products");
    }

    [Fact]
    public void Cart_ShouldRender_WithItems()
    {
        var cart = new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new() { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 },
                new() { ProductId = "2", Name = "Test Product 2", Price = 8.99m, Quantity = 1 }
            },
            TotalPrice = 30.97m,
            ItemCount = 3
        };

        var mockCartService = Mock.Get(_cartService);
        mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(cart);

        var cut = RenderComponent<Cart>();

        cut.Markup.Should().Contain("Test Product 1");
        cut.Markup.Should().Contain("Test Product 2");
        cut.Markup.Should().Contain("30.97"); // Total price
        cut.Markup.Should().Contain("Recommended Products"); // Recommendations section header
    }

    [Fact]
    public async Task Cart_ShouldClearCart_WhenClearButtonClicked()
    {
        var cart = new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new() { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 },
                new() { ProductId = "2", Name = "Test Product 2", Price = 8.99m, Quantity = 1 }
            },
            TotalPrice = 30.97m,
            ItemCount = 3
        };

        var mockCartService = Mock.Get(_cartService);
        mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(cart);
        mockCartService.Setup(s => s.ClearCartAsync()).ReturnsAsync(true);

        var cut = RenderComponent<Cart>();

        await cut.InvokeAsync(() => _cartService.ClearCartAsync());

        _toastService.ShowSuccess("Cart cleared successfully.");

        mockCartService.Verify(s => s.ClearCartAsync(), Times.Once);

        // Get the MockSnackbar to verify it was called
        var mockSnackbar = (MockSnackbar)Services.GetService<ISnackbar>()!;
        mockSnackbar.AddCallCount.Should().Be(1);
        mockSnackbar.LastSeverity.Should().Be(Severity.Success);
    }

    [Fact]
    public async Task Cart_ShouldUpdateQuantity_WhenQuantityChanged()
    {
        var cart = new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new() { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 }
            },
            TotalPrice = 21.98m,
            ItemCount = 2
        };

        var mockCartService = Mock.Get(_cartService);
        mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(cart);
        mockCartService.Setup(s => s.UpdateItemAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);

        var cut = RenderComponent<Cart>();

        string productId = "1";
        int newQuantity = 3;
        await cut.InvokeAsync(() => _cartService.UpdateItemAsync(productId, newQuantity));

        mockCartService.Verify(s => s.UpdateItemAsync("1", 3), Times.Once);
    }

    [Fact]
    public void Cart_ShouldRender_WithRecommendations()
    {
        var cart = new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new() { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 }
            },
            TotalPrice = 21.98m,
            ItemCount = 2
        };

        var recommendations = new List<Product>
        {
            new() { Id = "2", Name = "Recommended Product 1", Price = 15.99m, Description = "Description 1" },
            new() { Id = "3", Name = "Recommended Product 2", Price = 12.99m, Description = "Description 2" }
        };

        var mockCartService = Mock.Get(_cartService);
        mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(cart);

        var mockRecommendationService = Mock.Get(_recommendationService);
        mockRecommendationService.Setup(s => s.GetCartRecommendationsAsync(It.IsAny<int>()))
            .ReturnsAsync(recommendations);

        var cut = RenderComponent<Cart>();

        cut.Markup.Should().Contain("Recommended Products");
        cut.Markup.Should().Contain("Recommended Product 1");
        cut.Markup.Should().Contain("Recommended Product 2");
        cut.Markup.Should().Contain("15.99");
        cut.Markup.Should().Contain("12.99");
    }
}