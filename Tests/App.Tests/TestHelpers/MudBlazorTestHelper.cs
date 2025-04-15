using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Moq;

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

        var mockDialogService = CreateMockDialogService();
        context.Services.AddSingleton(mockDialogService.Object);

        var mockSnackbar = new MockSnackbar();
        context.Services.AddSingleton<ISnackbar>(mockSnackbar);
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
