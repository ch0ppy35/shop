using MudBlazor;

namespace Frontend.Services;

/// <summary>
/// Service for displaying confirmation dialogs using MudBlazor Dialog
/// </summary>
public class ConfirmService : IConfirmService
{
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Event raised when a confirmation dialog should be shown (kept for backward compatibility)
    /// </summary>
    public event Func<string, string, string, string, Task<bool>>? OnConfirmationRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmService"/> class
    /// </summary>
    /// <param name="dialogService">The MudBlazor dialog service</param>
    public ConfirmService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <summary>
    /// Shows a confirmation dialog
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the dialog</param>
    /// <param name="confirmType">The type of confirmation</param>
    /// <param name="confirmText">The text for the confirm button</param>
    /// <returns>True if confirmed, false otherwise</returns>
    public async Task<bool> ShowConfirmation(string message, string title = "Confirm", string confirmType = "Warning",
        string confirmText = "Confirm")
    {
        if (OnConfirmationRequested != null)
        {
            return await OnConfirmationRequested.Invoke(message, title, confirmType, confirmText);
        }

        var color = confirmType.ToLower() switch
        {
            "danger" => Color.Error,
            "warning" => Color.Warning,
            "info" => Color.Info,
            _ => Color.Primary
        };

        var parameters = new DialogParameters
        {
            { "ContentText", message },
            { "ButtonText", confirmText }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall
        };

        var result = await _dialogService.ShowMessageBox(title, message, yesText: confirmText, cancelText: "Cancel");

        return result == true;
    }
}