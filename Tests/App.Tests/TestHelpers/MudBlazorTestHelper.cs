using Bunit;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using System.Collections.Generic;

namespace App.Tests.TestHelpers;

/// <summary>
/// Helper class for setting up MudBlazor services in tests
/// </summary>
public static class MudBlazorTestHelper
{
    /// <summary>
    /// Adds MudBlazor services to the test context
    /// </summary>
    /// <param name="context">The test context</param>
    public static void AddMudBlazorTestServices(this TestContext context)
    {
        context.Services.AddMudServices();

        // Add mock services
        var mockDialogService = CreateMockDialogService();
        context.Services.AddSingleton(mockDialogService.Object);

        var mockSnackbar = new MockSnackbar();
        context.Services.AddSingleton<ISnackbar>(mockSnackbar);

        // Use our custom MockJSRuntime implementation instead of Moq
        var mockJsRuntime = new MockJSRuntime();
        context.Services.AddSingleton<IJSRuntime>(mockJsRuntime);
    }

    /// <summary>
    /// Adds all required services for cart tests
    /// </summary>
    public static void AddCartTestServices(this TestContext context)
    {
        // Add MudBlazor services first
        context.Services.AddMudServices();

        // Create mocks
        var mockCartService = new Mock<ICartService>();
        var mockRecommendationService = new Mock<IRecommendationService>();
        var mockDialogService = CreateMockDialogService();
        var mockSnackbar = new MockSnackbar();
        var mockJsRuntime = new MockJSRuntime();

        // Setup default behavior
        mockRecommendationService.Setup(s => s.GetCartRecommendationsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Frontend.Models.Product>());

        // Create services
        var toastService = new ToastService(mockSnackbar);
        var confirmService = new ConfirmService(mockDialogService.Object);

        // Register all services at once
        context.Services.AddSingleton(mockDialogService.Object);
        context.Services.AddSingleton<ISnackbar>(mockSnackbar);
        context.Services.AddSingleton<IJSRuntime>(mockJsRuntime);
        context.Services.AddSingleton(mockCartService.Object);
        context.Services.AddSingleton(mockRecommendationService.Object);
        context.Services.AddSingleton(toastService);
        context.Services.AddSingleton(confirmService);
        context.Services.AddSingleton<IConfirmService>(confirmService);
    }

    /// <summary>
    /// Creates a mock dialog service that returns a specified result
    /// </summary>
    /// <param name="dialogResult">The result to return from dialog operations</param>
    /// <returns>A mock dialog service</returns>
    public static Mock<IDialogService> CreateMockDialogService(bool? dialogResult = true)
    {
        var mockDialogService = new Mock<IDialogService>();

        mockDialogService
            .Setup(x => x.ShowMessageBox(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialogResult);

        return mockDialogService;
    }
}
