using FluentAssertions;
using Frontend.Services;

namespace App.Tests.Services;

public class ToastServiceTests
{
    private readonly ToastService _toastService = new();

    [Theory]
    [InlineData(ToastKind.Success)]
    [InlineData(ToastKind.Error)]
    [InlineData(ToastKind.Info)]
    [InlineData(ToastKind.Warning)]
    public void Show_ShouldAddToast_WithMatchingKind(ToastKind kind)
    {
        Action<string> show = kind switch
        {
            ToastKind.Success => m => _toastService.ShowSuccess(m),
            ToastKind.Error => m => _toastService.ShowError(m),
            ToastKind.Info => m => _toastService.ShowInfo(m),
            _ => m => _toastService.ShowWarning(m)
        };

        show("Hello");

        _toastService.Toasts.Should().ContainSingle()
            .Which.Should().Match<Toast>(t => t.Kind == kind && t.Message == "Hello");
    }

    [Fact]
    public void Show_ShouldRaiseChanged()
    {
        var raised = 0;
        _toastService.Changed += () => raised++;

        _toastService.ShowSuccess("Saved");

        raised.Should().Be(1);
    }

    [Fact]
    public void Dismiss_ShouldRemoveToast_AndRaiseChanged()
    {
        _toastService.ShowInfo("First", autoHideDelayMs: 0);
        _toastService.ShowInfo("Second", autoHideDelayMs: 0);
        var raised = 0;
        _toastService.Changed += () => raised++;

        _toastService.Dismiss(_toastService.Toasts[0].Id);

        _toastService.Toasts.Should().ContainSingle().Which.Message.Should().Be("Second");
        raised.Should().Be(1);
    }

    [Fact]
    public async Task Show_ShouldAutoDismiss_AfterDelay()
    {
        _toastService.ShowSuccess("Gone soon", autoHideDelayMs: 20);

        _toastService.Toasts.Should().HaveCount(1);
        await Task.Delay(200);

        _toastService.Toasts.Should().BeEmpty();
    }
}
