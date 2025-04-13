using System.Net;
using System.Text;
using System.Text.Json;
using Frontend.Services;
using Moq;
using Moq.Protected;

namespace App.Tests.Services;

public class CartServiceTests
{
    [Fact]
    public async Task GetCartAsync_ShouldReturnEmptyCart_WhenApiReturnsNull()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\":true,\"items\":null,\"totalPrice\":0,\"itemCount\":0}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var cartService = new CartService(httpClient);

        // Act
        var result = await cartService.GetCartAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalPrice);
        Assert.Equal(0, result.ItemCount);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public async Task GetCartAsync_ShouldReturnCart_WhenApiReturnsItems()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var responseContent = new
        {
            success = true,
            items = new[]
            {
                new { productId = "prod-1", name = "Product 1", price = 10.99m, quantity = 2, totalPrice = 21.98m },
                new { productId = "prod-2", name = "Product 2", price = 5.99m, quantity = 1, totalPrice = 5.99m }
            },
            totalPrice = 27.97m,
            itemCount = 3
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseContent), Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var cartService = new CartService(httpClient);

        // Act
        var result = await cartService.GetCartAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(27.97m, result.TotalPrice);
        Assert.Equal(3, result.ItemCount);
        Assert.False(result.IsEmpty);

        // Check first item
        var firstItem = result.Items[0];
        Assert.Equal("prod-1", firstItem.ProductId);
        Assert.Equal("Product 1", firstItem.Name);
        Assert.Equal(10.99m, firstItem.Price);
        Assert.Equal(2, firstItem.Quantity);
        Assert.Equal(21.98m, firstItem.TotalPrice);

        // Check second item
        var secondItem = result.Items[1];
        Assert.Equal("prod-2", secondItem.ProductId);
        Assert.Equal("Product 2", secondItem.Name);
        Assert.Equal(5.99m, secondItem.Price);
        Assert.Equal(1, secondItem.Quantity);
        Assert.Equal(5.99m, secondItem.TotalPrice);
    }

    [Fact]
    public async Task AddItemAsync_ShouldReturnTrue_WhenApiReturnsSuccess()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var cartService = new CartService(httpClient);

        // Act
        var result = await cartService.AddItemAsync("prod-1", 2);

        // Assert
        Assert.True(result);

        // Verify the request
        mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().EndsWith("/api/cart/items")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdateItemAsync_ShouldReturnTrue_WhenApiReturnsSuccess()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var cartService = new CartService(httpClient);

        // Act
        var result = await cartService.UpdateItemAsync("prod-1", 3);

        // Assert
        Assert.True(result);

        // Verify the request
        mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.ToString().EndsWith("/api/cart/items/prod-1")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldReturnTrue_WhenApiReturnsSuccess()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var cartService = new CartService(httpClient);

        // Act
        var result = await cartService.RemoveItemAsync("prod-1");

        // Assert
        Assert.True(result);

        // Verify the request
        mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().EndsWith("/api/cart/items/prod-1")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ClearCartAsync_ShouldReturnTrue_WhenApiReturnsSuccess()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var cartService = new CartService(httpClient);

        // Act
        var result = await cartService.ClearCartAsync();

        // Assert
        Assert.True(result);

        // Verify the request
        mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().EndsWith("/api/cart")),
                ItExpr.IsAny<CancellationToken>());
    }
}
