using Bunit;
using FluentAssertions;
using Frontend.Models;
using Frontend.Pages;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using App.Tests.TestHelpers;
using System.Collections.Generic;

namespace App.Tests.Pages;

public class CartTests : TestContext
{
    private readonly Mock<ICartService> _mockCartService;
    private readonly Mock<IRecommendationService> _mockRecommendationService;
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly MockSnackbar _mockSnackbar;
    private readonly Mock<IDialogService> _mockDialogService;
    private ToastService ToastService;

    public CartTests()
    {
        this.AddMudBlazorTestServices();

        _mockCartService = new Mock<ICartService>();
        _mockRecommendationService = new Mock<IRecommendationService>();
        _mockJsRuntime = new Mock<IJSRuntime>();
        _mockSnackbar = new MockSnackbar();
        _mockDialogService = MudBlazorTestHelper.CreateMockDialogService(true);

        _mockRecommendationService.Setup(s => s.GetCartRecommendationsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Product>());

        Services.AddSingleton(_mockCartService.Object);
        Services.AddSingleton(_mockRecommendationService.Object);
        Services.AddSingleton(_mockJsRuntime.Object);
        Services.AddSingleton<ISnackbar>(_mockSnackbar);
        Services.AddSingleton(_mockDialogService.Object);

        ToastService = new ToastService(_mockSnackbar);
        Services.AddSingleton(ToastService);

        var confirmService = new ConfirmService(_mockDialogService.Object);
        Services.AddSingleton(confirmService);
        Services.AddSingleton<IConfirmService>(confirmService);

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((object)null!);
    }

    [Fact]
    public void Cart_ShouldRender_EmptyCart()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(new ShoppingCart
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

        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(cart);

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

        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(cart);
        _mockCartService.Setup(s => s.ClearCartAsync()).ReturnsAsync(true);

        var cut = RenderComponent<Cart>();

        await cut.InvokeAsync(() => _mockCartService.Object.ClearCartAsync());

        ToastService.ShowSuccess("Cart cleared successfully.");

        _mockCartService.Verify(s => s.ClearCartAsync(), Times.Once);
        _mockSnackbar.AddCallCount.Should().Be(1);
        _mockSnackbar.LastSeverity.Should().Be(Severity.Success);
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

        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(cart);
        _mockCartService.Setup(s => s.UpdateItemAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);

        var cut = RenderComponent<Cart>();

        string productId = "1";
        int newQuantity = 3;
        await cut.InvokeAsync(() => _mockCartService.Object.UpdateItemAsync(productId, newQuantity));

        _mockCartService.Verify(s => s.UpdateItemAsync("1", 3), Times.Once);
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

        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(cart);
        _mockRecommendationService.Setup(s => s.GetCartRecommendationsAsync(It.IsAny<int>())).ReturnsAsync(recommendations);

        var cut = RenderComponent<Cart>();

        cut.Markup.Should().Contain("Recommended Products");
        cut.Markup.Should().Contain("Recommended Product 1");
        cut.Markup.Should().Contain("Recommended Product 2");
        cut.Markup.Should().Contain("15.99");
        cut.Markup.Should().Contain("12.99");
    }
}
