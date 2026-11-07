using App.Tests.TestHelpers;
using Bunit;
using FluentAssertions;
using Frontend.Models;
using Frontend.Pages;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace App.Tests.Pages;

public class CartTests : TestContext
{
    private readonly Mock<ICartService> _mockCartService;
    private readonly Mock<IRecommendationService> _mockRecommendationService;
    private readonly ToastService _toastService;
    private readonly IConfirmService _confirmService;

    public CartTests()
    {
        // Add all required services in one go
        this.AddCartTestServices();

        _mockCartService = Mock.Get(Services.GetRequiredService<ICartService>());
        _mockRecommendationService = Mock.Get(Services.GetRequiredService<IRecommendationService>());
        _toastService = Services.GetRequiredService<ToastService>();
        _confirmService = Services.GetRequiredService<IConfirmService>();
    }

    private static ShoppingCart TwoItemCart() => new()
    {
        Items = new List<CartItem>
        {
            new() { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 },
            new() { ProductId = "2", Name = "Test Product 2", Price = 8.99m, Quantity = 1 }
        },
        TotalPrice = 30.97m,
        ItemCount = 3
    };

    [Fact]
    public void Cart_ShouldRender_EmptyCart()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(new ShoppingCart());

        var cut = RenderComponent<Cart>();

        cut.Markup.Should().Contain("Your cart is empty");
        cut.Markup.Should().Contain("Browse products");
    }

    [Fact]
    public void Cart_ShouldRender_WithItems()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(TwoItemCart());

        var cut = RenderComponent<Cart>();

        cut.Markup.Should().Contain("Test Product 1");
        cut.Markup.Should().Contain("Test Product 2");
        cut.Markup.Should().Contain("30.97"); // Total price
        cut.Markup.Should().Contain("Recommended for this cart"); // Recommendations section header
    }

    [Fact]
    public void Cart_ShouldUpdateSharedItemCount_WhenLoaded()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(TwoItemCart());

        RenderComponent<Cart>();

        Services.GetRequiredService<CartState>().ItemCount.Should().Be(3);
    }

    [Fact]
    public void Cart_ShouldClearCart_WhenConfirmed()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(TwoItemCart());
        _mockCartService.Setup(s => s.ClearCartAsync()).ReturnsAsync(true);
        _confirmService.OnConfirmationRequested += (_, _, _, _) => Task.FromResult(true);

        var cut = RenderComponent<Cart>();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Empty cart").Click();

        _mockCartService.Verify(s => s.ClearCartAsync(), Times.Once);
        _toastService.Toasts.Should().ContainSingle(t => t.Message == "Emptied your cart.");
    }

    [Fact]
    public void Cart_ShouldNotClearCart_WhenCancelled()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(TwoItemCart());
        _confirmService.OnConfirmationRequested += (_, _, _, _) => Task.FromResult(false);

        var cut = RenderComponent<Cart>();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Empty cart").Click();

        _mockCartService.Verify(s => s.ClearCartAsync(), Times.Never);
    }

    [Fact]
    public void Cart_ShouldUpdateQuantity_WhenIncreaseClicked()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(TwoItemCart());
        _mockCartService.Setup(s => s.UpdateItemAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);

        var cut = RenderComponent<Cart>();
        cut.Find("button[aria-label='Increase quantity of Test Product 1']").Click();

        _mockCartService.Verify(s => s.UpdateItemAsync("1", 3), Times.Once);
    }

    [Fact]
    public void Cart_ShouldRemoveItem_WhenRemoveClicked()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(TwoItemCart());
        _mockCartService.Setup(s => s.RemoveItemAsync(It.IsAny<string>())).ReturnsAsync(true);

        var cut = RenderComponent<Cart>();
        cut.Find("button[aria-label='Remove Test Product 2']").Click();

        _mockCartService.Verify(s => s.RemoveItemAsync("2"), Times.Once);
        _toastService.Toasts.Should().ContainSingle(t => t.Message == "Removed Test Product 2 from your cart.");
    }

    [Fact]
    public void Cart_ShouldRender_WithRecommendations()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(TwoItemCart());
        _mockRecommendationService.Setup(s => s.GetCartRecommendationsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Product>
            {
                new() { Id = "3", Name = "Recommended Product 1", Price = 15.99m, Description = "Description 1", QuantityInStock = 5 },
                new() { Id = "4", Name = "Recommended Product 2", Price = 12.99m, Description = "Description 2", QuantityInStock = 5 }
            });

        var cut = RenderComponent<Cart>();

        cut.Markup.Should().Contain("Recommended for this cart");
        cut.Markup.Should().Contain("Recommended Product 1");
        cut.Markup.Should().Contain("Recommended Product 2");
        cut.Markup.Should().Contain("15.99");
        cut.Markup.Should().Contain("12.99");
    }
}
