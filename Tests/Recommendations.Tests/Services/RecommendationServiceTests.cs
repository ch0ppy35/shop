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

        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result.Should().BeInAscendingOrder(p => p.Price);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithCartItems_ShouldReturnSimilarProducts()
    {
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
            new ProductMessage { ProductId = "5", Name = "Product 5", Price = 5.00m } // Different price
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

        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().NotContain(p => p.ProductId == "1"); // Should not include items already in cart

        result[0].ProductId.Should().Be("3");
        result[1].ProductId.Should().Be("2");
    }

    [Fact]
    public async Task GetRecommendationsAsync_WhenProductServiceFails_ShouldReturnEmptyList()
    {
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

        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecommendationsAsync_WhenAllProductsAreInCart_ShouldReturnEmptyList()
    {
        var sessionId = "test-session";
        var cartItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Product 1", Price = 10.99m, Quantity = 1 },
            new CartItem { ProductId = "2", Name = "Product 2", Price = 15.99m, Quantity = 1 },
            new CartItem { ProductId = "3", Name = "Product 3", Price = 20.99m, Quantity = 1 }
        };
        var maxRecommendations = 5;

        var products = new List<ProductMessage>
        {
            new ProductMessage { ProductId = "1", Name = "Product 1", Price = 10.99m },
            new ProductMessage { ProductId = "2", Name = "Product 2", Price = 15.99m },
            new ProductMessage { ProductId = "3", Name = "Product 3", Price = 20.99m }
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

        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithLargePriceDifferences_ShouldPrioritizeSimilarPrices()
    {
        var sessionId = "test-session";
        var cartItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Product 1", Price = 100.00m, Quantity = 1 }
        };
        var maxRecommendations = 3;

        var products = new List<ProductMessage>
        {
            new ProductMessage { ProductId = "1", Name = "Product 1", Price = 100.00m }, // Already in cart
            new ProductMessage { ProductId = "2", Name = "Product 2", Price = 95.00m }, // Close price
            new ProductMessage { ProductId = "3", Name = "Product 3", Price = 105.00m }, // Close price
            new ProductMessage { ProductId = "4", Name = "Product 4", Price = 10.00m }, // Far price
            new ProductMessage { ProductId = "5", Name = "Product 5", Price = 500.00m } // Far price
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

        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().NotContain(p => p.ProductId == "1"); // Should not include items already in cart

        // The items with highest similarity score should be first
        // Since 95.00 and 105.00 have the same absolute difference from 100.00,
        // either could be first depending on the implementation
        result.Should().Contain(p => p.ProductId == "2"); // 95.00
        result.Should().Contain(p => p.ProductId == "3"); // 105.00

        // These should be included but with lower priority
        if (result.Count > 2)
        {
            var lastItem = result.Last();
            lastItem.ProductId.Should().BeOneOf("4", "5"); // Either the very low or very high price item
        }
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithLargeDataset_ShouldLimitResults()
    {
        var sessionId = "test-session";
        var cartItems = new List<CartItem>
        {
            new CartItem { ProductId = "1", Name = "Product 1", Price = 50.00m, Quantity = 1 }
        };
        var maxRecommendations = 5;

        // Create a large list of products
        var products = new List<ProductMessage>();
        products.Add(new ProductMessage { ProductId = "1", Name = "Product 1", Price = 50.00m }); // Already in cart

        // Add 100 more products with varying prices
        for (int i = 2; i <= 101; i++)
        {
            products.Add(new ProductMessage
            {
                ProductId = i.ToString(),
                Name = $"Product {i}",
                Price = 50.00m + (i % 10) // Prices will vary from 50 to 59
            });
        }

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

        var result = await _service.GetRecommendationsAsync(sessionId, cartItems, maxRecommendations);

        result.Should().NotBeNull();
        result.Should().HaveCount(maxRecommendations); // Should limit to maxRecommendations
        result.Should().NotContain(p => p.ProductId == "1"); // Should not include items already in cart
    }
}