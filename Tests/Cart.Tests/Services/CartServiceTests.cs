using Cart.Services;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cart.Tests.Services;

public class CartServiceTests
{
    private const string SessionId = "test-session";
    private readonly Mock<ICartStore> _storeMock = new();
    private readonly CartService _service;

    public CartServiceTests()
    {
        _service = new CartService(NullLogger<CartService>.Instance, _storeMock.Object);
    }

    private static List<CartItem> TwoLines() => new()
    {
        new CartItem { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 },
        new CartItem { ProductId = "2", Name = "Test Product 2", Price = 5.99m, Quantity = 1 }
    };

    [Fact]
    public async Task GetCartAsync_ShouldReturnEmptyCart_WhenCartDoesNotExist()
    {
        _storeMock.Setup(s => s.GetItemsAsync(SessionId)).ReturnsAsync(new List<CartItem>());

        var result = await _service.GetCartAsync(SessionId);

        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(SessionId);
        result.Items.Should().BeEmpty();
        result.TotalPrice.Should().Be(0);
        result.ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCartAsync_ShouldReturnCartWithTotals()
    {
        _storeMock.Setup(s => s.GetItemsAsync(SessionId)).ReturnsAsync(TwoLines());

        var result = await _service.GetCartAsync(SessionId);

        result.Success.Should().BeTrue();
        result.Items.Should().HaveCount(2);
        result.TotalPrice.Should().Be(27.97m);
        result.ItemCount.Should().Be(3);
    }

    [Fact]
    public async Task AddItemAsync_ShouldReturnTheCartTheStoreWrote()
    {
        var item = new CartItem { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 };
        _storeMock.Setup(s => s.AddItemAsync(SessionId, item)).ReturnsAsync(TwoLines());

        var result = await _service.AddItemAsync(SessionId, item);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Item added to cart successfully");
        result.ItemCount.Should().Be(3);
        _storeMock.Verify(s => s.GetItemsAsync(It.IsAny<string>()), Times.Never,
            "the write already returns the cart, so there's no second read");
    }

    [Fact]
    public async Task UpdateItemAsync_ShouldReturnUpdatedCart_WhenItemIsInCart()
    {
        _storeMock.Setup(s => s.SetQuantityAsync(SessionId, "1", 5)).ReturnsAsync(TwoLines());

        var result = await _service.UpdateItemAsync(SessionId, "1", 5);

        result.Success.Should().BeTrue();
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateItemAsync_ShouldFailWithCurrentCart_WhenItemIsNotInCart()
    {
        _storeMock.Setup(s => s.SetQuantityAsync(SessionId, "3", 1)).ReturnsAsync((List<CartItem>?)null);
        _storeMock.Setup(s => s.GetItemsAsync(SessionId)).ReturnsAsync(TwoLines());

        var result = await _service.UpdateItemAsync(SessionId, "3", 1);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found in cart");
        result.ItemCount.Should().Be(3);
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldReturnRemainingCart_WhenItemIsInCart()
    {
        var remaining = TwoLines().Skip(1).ToList();
        _storeMock.Setup(s => s.RemoveItemAsync(SessionId, "1")).ReturnsAsync(remaining);

        var result = await _service.RemoveItemAsync(SessionId, "1");

        result.Success.Should().BeTrue();
        result.Items.Should().ContainSingle(i => i.ProductId == "2");
        result.TotalPrice.Should().Be(5.99m);
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldFail_WhenItemIsNotInCart()
    {
        _storeMock.Setup(s => s.RemoveItemAsync(SessionId, "3")).ReturnsAsync((List<CartItem>?)null);
        _storeMock.Setup(s => s.GetItemsAsync(SessionId)).ReturnsAsync(TwoLines());

        var result = await _service.RemoveItemAsync(SessionId, "3");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found in cart");
    }

    [Fact]
    public async Task ClearCartAsync_ShouldReturnEmptyCart()
    {
        var result = await _service.ClearCartAsync(SessionId);

        result.Success.Should().BeTrue();
        result.Items.Should().BeEmpty();
        _storeMock.Verify(s => s.ClearAsync(SessionId), Times.Once);
    }

    [Fact]
    public async Task GetCartAsync_ShouldReturnError_WhenStoreThrows()
    {
        _storeMock.Setup(s => s.GetItemsAsync(SessionId))
            .ThrowsAsync(new InvalidOperationException("Redis connection error"));

        var result = await _service.GetCartAsync(SessionId);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Redis connection error");
        result.Items.Should().BeEmpty();
    }
}
