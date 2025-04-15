using Cart.Services;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cart.Tests.Services;

public class CartServiceTests
{
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly Mock<IRedisService> _redisServiceMock;
    private readonly CartService _service;

    public CartServiceTests()
    {
        _loggerMock = new Mock<ILogger<CartService>>();
        _redisServiceMock = new Mock<IRedisService>();

        // We can't mock CartTtl property since it's not virtual, so we'll just use it directly
        // in our tests and avoid setting up expectations for it

        _service = new CartService(_loggerMock.Object, _redisServiceMock.Object);
    }

    [Fact]
    public async Task GetCartAsync_ShouldReturnEmptyCart_WhenCartDoesNotExist()
    {
        // Arrange
        var sessionId = "test-session";
        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<CartItem>?)null);

        // Act
        var result = await _service.GetCartAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalPrice.Should().Be(0);
        result.ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCartAsync_ShouldReturnCart_WhenCartExists()
    {
        // Arrange
        var sessionId = "test-session";
        var cartItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 },
            new CartItem { ProductId = "2", Name = "Test Product 2", Price = 5.99m, Quantity = 1 }
        };

        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartItems);

        // Act
        var result = await _service.GetCartAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        result.Items.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalPrice.Should().Be(27.97m); // (10.99 * 2) + 5.99
        result.ItemCount.Should().Be(3); // 2 + 1
    }

    [Fact]
    public async Task AddItemAsync_ShouldAddNewItem_WhenItemDoesNotExistInCart()
    {
        // Arrange
        var sessionId = "test-session";
        var existingItems = new List<CartItem>();
        var newItem = new CartItem { ProductId = "1", Name = "Test Product", Price = 10.99m, Quantity = 2 };

        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItems);

        _redisServiceMock.Setup(r => r.SetAsync(
                It.IsAny<string>(),
                It.Is<List<CartItem>>(items => items.Count == 1 && items[0].ProductId == "1"),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddItemAsync(sessionId, newItem);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        result.Items.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be("1");
        result.Items[0].Quantity.Should().Be(2);
        result.TotalPrice.Should().Be(21.98m); // 10.99 * 2
        result.ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task AddItemAsync_ShouldUpdateQuantity_WhenItemExistsInCart()
    {
        // Arrange
        var sessionId = "test-session";
        var existingItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Test Product", Price = 10.99m, Quantity = 2 }
        };
        var newItem = new CartItem { ProductId = "1", Name = "Test Product", Price = 10.99m, Quantity = 3 };

        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItems);

        _redisServiceMock.Setup(r => r.SetAsync(
                It.IsAny<string>(),
                It.Is<List<CartItem>>(items => items.Count == 1 && items[0].Quantity == 5),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddItemAsync(sessionId, newItem);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        result.Items.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be("1");
        result.Items[0].Quantity.Should().Be(5); // 2 + 3
        result.TotalPrice.Should().Be(54.95m); // 10.99 * 5
        result.ItemCount.Should().Be(5);
    }

    [Fact]
    public async Task UpdateItemAsync_ShouldUpdateQuantity_WhenItemExistsInCart()
    {
        // Arrange
        var sessionId = "test-session";
        var existingItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Test Product", Price = 10.99m, Quantity = 2 }
        };

        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItems);

        _redisServiceMock.Setup(r => r.SetAsync(
                It.IsAny<string>(),
                It.Is<List<CartItem>>(items => items.Count == 1 && items[0].Quantity == 5),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateItemAsync(sessionId, "1", 5);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        result.Items.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be("1");
        result.Items[0].Quantity.Should().Be(5);
        result.TotalPrice.Should().Be(54.95m); // 10.99 * 5
        result.ItemCount.Should().Be(5);
    }

    [Fact]
    public async Task UpdateItemAsync_ShouldReturnFalse_WhenItemDoesNotExistInCart()
    {
        // Arrange
        var sessionId = "test-session";
        var existingItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Test Product", Price = 10.99m, Quantity = 2 }
        };

        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItems);

        // Act
        var result = await _service.UpdateItemAsync(sessionId, "2", 5);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldRemoveItem_WhenItemExistsInCart()
    {
        // Arrange
        var sessionId = "test-session";
        var existingItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 },
            new CartItem { ProductId = "2", Name = "Test Product 2", Price = 5.99m, Quantity = 1 }
        };

        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItems);

        _redisServiceMock.Setup(r => r.SetAsync(
                It.IsAny<string>(),
                It.Is<List<CartItem>>(items => items.Count == 1 && items[0].ProductId == "2"),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RemoveItemAsync(sessionId, "1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        result.Items.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be("2");
        result.TotalPrice.Should().Be(5.99m);
        result.ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldReturnFalse_WhenItemDoesNotExistInCart()
    {
        // Arrange
        var sessionId = "test-session";
        var existingItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Test Product", Price = 10.99m, Quantity = 2 }
        };

        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItems);

        // Act
        var result = await _service.RemoveItemAsync(sessionId, "2");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ClearCartAsync_ShouldClearCart()
    {
        // Arrange
        var sessionId = "test-session";
        var existingItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Test Product 1", Price = 10.99m, Quantity = 2 },
            new CartItem { ProductId = "2", Name = "Test Product 2", Price = 5.99m, Quantity = 1 }
        };

        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItems);

        _redisServiceMock.Setup(r => r.SetAsync(
                It.IsAny<string>(),
                It.Is<List<CartItem>>(items => items.Count == 0),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ClearCartAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().Be(sessionId);
        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalPrice.Should().Be(0);
        result.ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCartAsync_ShouldHandleRedisException()
    {
        // Arrange
        var sessionId = "test-session";
        _redisServiceMock.Setup(r => r.GetAsync<List<CartItem>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis connection error"));

        // Act
        var result = await _service.GetCartAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Redis connection error");
        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }
}
