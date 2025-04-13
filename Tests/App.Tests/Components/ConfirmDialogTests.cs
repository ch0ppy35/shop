using Bunit;
using FluentAssertions;
using Frontend.Components;
using Frontend.Services;
using Microsoft.AspNetCore.Components;

namespace App.Tests.Components;

public class ConfirmDialogTests : TestContext
{
    [Fact]
    public void ConfirmDialog_ShouldNotBeVisible_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<ConfirmDialog>();

        // Assert
        cut.Markup.Should().NotContain("modal-dialog");
    }

    [Fact]
    public void ConfirmDialog_ShouldBeVisible_WhenShown()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Message, "Test message");
            parameters.Add(p => p.IsVisible, true);
        });

        // Assert
        cut.Markup.Should().Contain("modal-dialog");
        cut.Markup.Should().Contain("Test message");
    }

    [Fact]
    public void ConfirmDialog_ShouldHaveCorrectType_WhenSpecified()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        cut.SetParametersAndRender(parameters =>
        {
            parameters.Add(p => p.Message, "Test message");
            parameters.Add(p => p.Title, "Warning");
            parameters.Add(p => p.Type, ConfirmDialog.ConfirmType.Warning);
            parameters.Add(p => p.IsVisible, true);
        });

        // Assert
        cut.Markup.Should().Contain("bg-warning");
        cut.Markup.Should().Contain("btn-warning");
        cut.Markup.Should().Contain("Warning");
    }

    [Fact]
    public async Task ConfirmDialog_ShouldInvokeCallback_WhenConfirmed()
    {
        // Arrange
        bool? result = null;
        var cut = RenderComponent<ConfirmDialog>(parameters =>
        {
            parameters.Add(p => p.Message, "Test message");
            parameters.Add(p => p.IsVisible, true);
            parameters.Add(p => p.OnResult, EventCallback.Factory.Create<bool>(this, (bool r) => { result = r; }));
        });

        // Act
        var confirmButton = cut.Find("button.btn-primary, button.btn-warning, button.btn-danger, button.btn-info");
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmDialog_ShouldInvokeCallback_WhenCancelled()
    {
        // Arrange
        bool? result = null;
        var cut = RenderComponent<ConfirmDialog>(parameters =>
        {
            parameters.Add(p => p.Message, "Test message");
            parameters.Add(p => p.IsVisible, true);
            parameters.Add(p => p.OnResult, EventCallback.Factory.Create<bool>(this, (bool r) => { result = r; }));
        });

        // Act
        var cancelButton = cut.Find("button.btn-secondary");
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ConfirmService_ShouldRaiseEvent_WhenCalled()
    {
        // Arrange
        var confirmService = new ConfirmService();
        string? capturedMessage = null;
        string? capturedTitle = null;
        string? capturedType = null;
        string? capturedConfirmText = null;

        confirmService.OnConfirmationRequested += (message, title, type, confirmText) =>
        {
            capturedMessage = message;
            capturedTitle = title;
            capturedType = type;
            capturedConfirmText = confirmText;
            return Task.FromResult(true);
        };

        // Act
        var _ = confirmService.ShowConfirmation("Confirm message", "Confirm Title", "Danger", "Delete");

        // Assert
        capturedMessage.Should().Be("Confirm message");
        capturedTitle.Should().Be("Confirm Title");
        capturedType.Should().Be("Danger");
        capturedConfirmText.Should().Be("Delete");
    }
}
