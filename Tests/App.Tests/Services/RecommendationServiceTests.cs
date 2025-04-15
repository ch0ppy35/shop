using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Frontend.Models;
using Frontend.Services;
using Moq;
using Moq.Protected;

namespace App.Tests.Services;

public class RecommendationServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _service = new RecommendationService(_httpClient);
    }

    [Fact]
    public async Task GetCartRecommendationsAsync_ShouldReturnRecommendations_WhenApiReturnsSuccess()
    {
        var recommendations = new List<object>
        {
            new
            {
                productId = "1", name = "Product 1", description = "Description 1", price = 10.99m, sku = "SKU1",
                location = "Location 1", quantityInStock = 10
            },
            new
            {
                productId = "2", name = "Product 2", description = "Description 2", price = 15.99m, sku = "SKU2",
                location = "Location 2", quantityInStock = 20
            }
        };

        var response = new
        {
            success = true,
            message = "Recommendations retrieved successfully",
            recommendations = recommendations
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var result = await _service.GetCartRecommendationsAsync(4);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("1", result[0].Id);
        Assert.Equal("Product 1", result[0].Name);
        Assert.Equal(10.99m, result[0].Price);
        Assert.Equal("2", result[1].Id);
        Assert.Equal("Product 2", result[1].Name);
        Assert.Equal(15.99m, result[1].Price);
    }

    [Fact]
    public async Task GetCartRecommendationsAsync_ShouldReturnEmptyList_WhenApiReturnsError()
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Error"));

        var result = await _service.GetCartRecommendationsAsync(4);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCartRecommendationsAsync_ShouldReturnEmptyList_WhenApiReturnsNull()
    {
        var response = new
        {
            success = true,
            message = "Recommendations retrieved successfully",
            recommendations = (object?)null
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var result = await _service.GetCartRecommendationsAsync(4);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}