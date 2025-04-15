using FluentAssertions;
using Frontend.Services;
using Moq;
using MudBlazor;

namespace App.Tests.Services;

public class ConfirmServiceTests
{
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly ConfirmService _confirmService;

    public ConfirmServiceTests()
    {
        _mockDialogService = new Mock<IDialogService>();
        _confirmService = new ConfirmService(_mockDialogService.Object);
    }

    [Fact]
    public async Task ShowConfirmation_ShouldCallDialogService_AndReturnResult()
    {
        _mockDialogService
            .Setup(d => d.ShowMessageBox(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DialogOptions>()))
            .ReturnsAsync(true);

        var result = await _confirmService.ShowConfirmation(
            "Are you sure?",
            "Confirm",
            "Warning",
            "Yes");

        result.Should().BeTrue();
        _mockDialogService.Verify(d => d.ShowMessageBox(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DialogOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task ShowConfirmation_ShouldUseCorrectColor_ForDangerType()
    {
        _mockDialogService
            .Setup(d => d.ShowMessageBox(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DialogOptions>()))
            .ReturnsAsync(true);

        var result = await _confirmService.ShowConfirmation(
            "Are you sure?",
            "Confirm",
            "Danger",
            "Delete");

        result.Should().BeTrue();
        _mockDialogService.Verify(d => d.ShowMessageBox(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DialogOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task ShowConfirmation_ShouldUseEventHandler_WhenRegistered()
    {
        bool eventHandlerCalled = false;
        string? capturedMessage = null;

        _confirmService.OnConfirmationRequested += (message, title, type, confirmText) =>
        {
            eventHandlerCalled = true;
            capturedMessage = message;
            return Task.FromResult(true);
        };

        var result = await _confirmService.ShowConfirmation("Test message");

        eventHandlerCalled.Should().BeTrue();
        capturedMessage.Should().Be("Test message");
        result.Should().BeTrue();

        _mockDialogService.Verify(d => d.ShowMessageBox(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DialogOptions>()),
            Times.Never);
    }
}