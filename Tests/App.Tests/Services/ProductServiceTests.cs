using System.Net;
using Frontend.Models;
using Frontend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Moq;
using Moq.Protected;
using App.Tests.TestHelpers;

namespace App.Tests.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task GetProductsAsync_ShouldAddSessionIdHeader_ToRequest()
    {
        // Arrange
        var sessionId = "test-session-id";
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Setup the mock HTTP handler to capture the request headers
        HttpRequestMessage? capturedRequest = null;

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{
                    ""success"": true,
                    ""products"": [
                        {
                            ""productId"": ""1"",
                            ""name"": ""Test Product"",
                            ""description"": ""Test Description"",
                            ""price"": 9.99,
                            ""quantity"": 10,
                            ""sku"": ""SKU123"",
                            ""location"": ""Warehouse A"",
                            ""quantityInStock"": 10,
                            ""reorderThreshold"": 5
                        }
                    ],
                    ""totalCount"": 1,
                    ""pageNumber"": 1,
                    ""pageSize"": 5,
                    ""totalPages"": 1
                }")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        // Create a mock JavaScript interop that returns a session ID
        var mockJsInterop = new MockJavaScriptInterop();
        await mockJsInterop.SetSessionId(sessionId);

        // Setup configuration
        var configurationMock = new Mock<IConfiguration>();
        configurationMock
            .Setup(c => c[It.Is<string>(s => s == "ApiBaseUrl")])
            .Returns("http://localhost:8080");

        // Create the services
        var jsRuntimeMock = new Mock<IJSRuntime>();
        var configService = new ConfigurationService(jsRuntimeMock.Object, configurationMock.Object);
        var sessionService = new SessionService(mockJsInterop);
        var sessionHandler = new SessionHttpMessageHandler(sessionService);

        // Set the inner handler
        var handlerField = typeof(DelegatingHandler).GetField("_innerHandler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handlerField?.SetValue(sessionHandler, mockHttpMessageHandler.Object);

        var clientWithSessionHandler = new HttpClient(sessionHandler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var productService = new ProductService(clientWithSessionHandler, configService);

        // Act
        var result = await productService.GetProductsAsync();

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("X-Session-ID"));
        Assert.Equal(sessionId, capturedRequest.Headers.GetValues("X-Session-ID").First());

        // Also verify the result
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Test Product", result.Items.First().Name);
    }
}
