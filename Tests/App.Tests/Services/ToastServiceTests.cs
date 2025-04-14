using FluentAssertions;
using Frontend.Services;
using MudBlazor;
using App.Tests.TestHelpers;

namespace App.Tests.Services;

public class ToastServiceTests
{
    private readonly MockSnackbar _mockSnackbar;
    private readonly ToastService _toastService;

    public ToastServiceTests()
    {
        _mockSnackbar = new MockSnackbar();
        _toastService = new ToastService(_mockSnackbar);
    }

    [Fact]
    public void ShowSuccess_ShouldCallSnackbar_WithSuccessSeverity()
    {
        // Act
        _toastService.ShowSuccess("Success message");

        // Assert
        _mockSnackbar.AddCallCount.Should().Be(1);
        _mockSnackbar.LastMessage.Should().Be("Success message");
        _mockSnackbar.LastSeverity.Should().Be(Severity.Success);
    }

    [Fact]
    public void ShowError_ShouldCallSnackbar_WithErrorSeverity()
    {
        // Act
        _toastService.ShowError("Error message");

        // Assert
        _mockSnackbar.AddCallCount.Should().Be(1);
        _mockSnackbar.LastMessage.Should().Be("Error message");
        _mockSnackbar.LastSeverity.Should().Be(Severity.Error);
    }

    [Fact]
    public void ShowInfo_ShouldCallSnackbar_WithInfoSeverity()
    {
        // Act
        _toastService.ShowInfo("Info message");

        // Assert
        _mockSnackbar.AddCallCount.Should().Be(1);
        _mockSnackbar.LastMessage.Should().Be("Info message");
        _mockSnackbar.LastSeverity.Should().Be(Severity.Info);
    }

    [Fact]
    public void ShowWarning_ShouldCallSnackbar_WithWarningSeverity()
    {
        // Act
        _toastService.ShowWarning("Warning message");

        // Assert
        _mockSnackbar.AddCallCount.Should().Be(1);
        _mockSnackbar.LastMessage.Should().Be("Warning message");
        _mockSnackbar.LastSeverity.Should().Be(Severity.Warning);
    }
}
