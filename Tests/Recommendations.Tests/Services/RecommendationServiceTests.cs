using Common.Messaging;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Recommendations.Services;
using Xunit;

namespace Recommendations.Tests.Services;

public class RecommendationServiceTests
{
    private readonly Mock<ILogger<RecommendationService>> _mockLogger;
    private readonly Mock<INatsService> _mockNatsService;
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        _mockLogger = new Mock<ILogger<RecommendationService>>();
        _mockNatsService = new Mock<INatsService>();
        _service = new RecommendationService(_mockLogger.Object, _mockNatsService.Object);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithEmptyCart_ShouldReturnPopularProducts()
    {
        // Arrange
        var sessionId = "test-session";
        var cartItems = new List<CartItem>();
        var maxRecommendations = 5;

        var products = new List<ProductMessage>
        {
            new ProductMessage { ProductId = "1", Name = "Product 1", Price = 10.99m },
            new ProductMessage { ProductId = "2", Name = "Product 2", Price = 15.99m },
            new ProductMessage { ProductId = "3", Name = "Product 3", Price = 20.99m },
            new ProductMessage { ProductId = "4", Name = "Product 4", Price = 25.99m },
            new ProductMessage { ProductId = "5", Name = "Product 5", Price = 30.99m }
        };

        var productListResponse = new ProductListResponse
        {
            Success = true,
            Products = products
        };

        _mockNatsService
            .Setup(x => x.RequestAsync<ProductMessage, ProductListResponse>(
                "products.getall",
                It.IsAny<ProductMessage>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(productListResponse);

        // Act
        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result.Should().BeInAscendingOrder(p => p.Price);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithCartItems_ShouldReturnSimilarProducts()
    {
        // Arrange
        var sessionId = "test-session";
        var cartItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Product 1", Price = 20.00m, Quantity = 1 }
        };
        var maxRecommendations = 3;

        var products = new List<ProductMessage>
        {
            new ProductMessage { ProductId = "1", Name = "Product 1", Price = 20.00m }, // Already in cart
            new ProductMessage { ProductId = "2", Name = "Product 2", Price = 21.99m }, // Similar price
            new ProductMessage { ProductId = "3", Name = "Product 3", Price = 19.99m }, // Similar price
            new ProductMessage { ProductId = "4", Name = "Product 4", Price = 50.00m }, // Different price
            new ProductMessage { ProductId = "5", Name = "Product 5", Price = 5.00m }   // Different price
        };

        var productListResponse = new ProductListResponse
        {
            Success = true,
            Products = products
        };

        _mockNatsService
            .Setup(x => x.RequestAsync<ProductMessage, ProductListResponse>(
                "products.getall",
                It.IsAny<ProductMessage>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(productListResponse);

        // Act
        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().NotContain(p => p.ProductId == "1"); // Should not include items already in cart

        // First recommendations should be the ones with similar prices
        result[0].ProductId.Should().Be("3");
        result[1].ProductId.Should().Be("2");
    }

    [Fact]
    public async Task GetRecommendationsAsync_WhenProductServiceFails_ShouldReturnEmptyList()
    {
        // Arrange
        var sessionId = "test-session";
        var cartItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Product 1", Price = 20.00m, Quantity = 1 }
        };
        var maxRecommendations = 3;

        _mockNatsService
            .Setup(x => x.RequestAsync<ProductMessage, ProductListResponse>(
                "products.getall",
                It.IsAny<ProductMessage>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductListResponse)null!);

        // Act
        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
