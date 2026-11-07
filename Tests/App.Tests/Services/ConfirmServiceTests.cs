using FluentAssertions;
using Frontend.Services;

namespace App.Tests.Services;

public class ConfirmServiceTests
{
    private readonly ConfirmService _confirmService = new();

    [Fact]
    public async Task ShowConfirmation_ShouldReturnFalse_WhenNoHostIsListening()
    {
        var result = await _confirmService.ShowConfirmation("Are you sure?");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShowConfirmation_ShouldReturnHostAnswer(bool answer)
    {
        _confirmService.OnConfirmationRequested += (_, _, _, _) => Task.FromResult(answer);

        var result = await _confirmService.ShowConfirmation("Are you sure?");

        result.Should().Be(answer);
    }

    [Fact]
    public async Task ShowConfirmation_ShouldPassAllArguments_ToHost()
    {
        string[]? captured = null;
        _confirmService.OnConfirmationRequested += (message, title, type, confirmText) =>
        {
            captured = [message, title, type, confirmText];
            return Task.FromResult(true);
        };

        await _confirmService.ShowConfirmation("Delete it?", "Delete", "Danger", "Delete product");

        captured.Should().Equal("Delete it?", "Delete", "Danger", "Delete product");
    }
}
