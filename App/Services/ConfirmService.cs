namespace Frontend.Services;

/// <summary>
/// Service for requesting confirmation; the ConfirmHost component answers by showing a dialog
/// </summary>
public class ConfirmService : IConfirmService
{
    /// <summary>
    /// Raised when a confirmation dialog should be shown
    /// </summary>
    public event Func<string, string, string, string, Task<bool>>? OnConfirmationRequested;

    /// <summary>
    /// Shows a confirmation dialog
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the dialog</param>
    /// <param name="confirmType">The type of confirmation (Danger, Warning, Info)</param>
    /// <param name="confirmText">The text for the confirm button</param>
    /// <returns>True if confirmed, false otherwise or when nothing is listening</returns>
    public async Task<bool> ShowConfirmation(string message, string title = "Confirm", string confirmType = "Warning",
        string confirmText = "Confirm")
    {
        if (OnConfirmationRequested == null)
        {
            Console.WriteLine("Confirmation requested but no ConfirmHost is rendered");
            return false;
        }

        return await OnConfirmationRequested.Invoke(message, title, confirmType, confirmText);
    }
}
