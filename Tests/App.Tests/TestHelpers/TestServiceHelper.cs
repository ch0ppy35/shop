using Bunit;
using Frontend.Models;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace App.Tests.TestHelpers;

/// <summary>
/// Helper class for registering the app's UI services in tests
/// </summary>
public static class TestServiceHelper
{
    /// <summary>
    /// Adds the services every layout component needs: JS runtime, toasts, confirmations,
    /// request timeline and a cart state backed by an empty mock cart
    /// </summary>
    /// <param name="context">The test context</param>
    /// <returns>The mock cart service, for further setup</returns>
    public static Mock<ICartService> AddAppTestServices(this TestContext context)
    {
        var mockCartService = new Mock<ICartService>();
        mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(new ShoppingCart());

        // Use our custom MockJSRuntime implementation instead of Moq
        context.Services.AddSingleton<IJSRuntime>(new MockJSRuntime());
        context.Services.AddSingleton(mockCartService.Object);
        context.Services.AddSingleton<CartState>();
        context.Services.AddSingleton<ToastService>();
        context.Services.AddSingleton<ConfirmService>();
        context.Services.AddSingleton<IConfirmService>(sp => sp.GetRequiredService<ConfirmService>());
        context.Services.AddSingleton<RequestTimeline>();

        return mockCartService;
    }

    /// <summary>
    /// Adds all required services for cart page tests
    /// </summary>
    public static void AddCartTestServices(this TestContext context)
    {
        context.AddAppTestServices();

        var mockRecommendationService = new Mock<IRecommendationService>();
        mockRecommendationService.Setup(s => s.GetCartRecommendationsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Product>());

        context.Services.AddSingleton(mockRecommendationService.Object);
    }
}
