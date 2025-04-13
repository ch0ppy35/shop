using Bunit;
using FluentAssertions;
using Frontend.Components;
using Frontend.Services;

namespace App.Tests.Components;

public class ToastTests : TestContext
{
    [Fact]
    public void Toast_ShouldNotBeVisible_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<Toast>();

        // Assert
        cut.Markup.Should().NotContain("toast-container");
    }

    [Fact]
    public void Toast_ShouldBeVisible_WhenShown()
    {
        // Arrange
        var cut = RenderComponent<Toast>();

        // Act - set the properties directly instead of calling Show()
        cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Message, "Test message");
            parameters.Add(p => p.IsVisible, true);
        });

        // Assert
        cut.Markup.Should().Contain("toast-container");
        cut.Markup.Should().Contain("Test message");
    }

    [Fact]
    public void Toast_ShouldHaveCorrectType_WhenSpecified()
    {
        // Arrange
        var cut = RenderComponent<Toast>();

        // Act
        cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Message, "Test message");
            parameters.Add(p => p.Title, "Warning");
            parameters.Add(p => p.Type, Toast.ToastType.Warning);
            parameters.Add(p => p.IsVisible, true);
        });

        // Assert
        cut.Markup.Should().Contain("bg-warning");
        cut.Markup.Should().Contain("Warning");
    }

    [Fact]
    public void Toast_ShouldClose_WhenCloseButtonClicked()
    {
        // Arrange
        var cut = RenderComponent<Toast>(parameters =>
        {
            parameters.Add(p => p.Message, "Test message");
            parameters.Add(p => p.IsVisible, true);
        });

        // Act
        cut.Find("button.btn-close").Click();

        // Assert
        cut.Markup.Should().NotContain("toast-container");
    }

    [Fact]
    public void ToastService_ShouldRaiseEvent_WhenCalled()
    {
        // Arrange
        var toastService = new ToastService();
        string? capturedMessage = null;
        string? capturedTitle = null;
        string? capturedType = null;
        int capturedDelay = 0;

        toastService.OnShow += (message, title, type, delay) =>
        {
            capturedMessage = message;
            capturedTitle = title;
            capturedType = type;
            capturedDelay = delay;
        };

        // Act
        toastService.ShowSuccess("Success message");

        // Assert
        capturedMessage.Should().Be("Success message");
        capturedTitle.Should().Be("Success");
        capturedType.Should().Be("Success");
        capturedDelay.Should().Be(3000);
    }
}
