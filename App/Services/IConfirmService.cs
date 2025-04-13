namespace Frontend.Services;

/// <summary>
/// Interface for confirmation dialog service
/// </summary>
public interface IConfirmService
{
    /// <summary>
    /// Event raised when a confirmation dialog should be shown
    /// </summary>
    event Func<string, string, string, string, Task<bool>>? OnConfirmationRequested;

    /// <summary>
    /// Shows a confirmation dialog
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the dialog</param>
    /// <param name="confirmType">The type of confirmation</param>
    /// <param name="confirmText">The text for the confirm button</param>
    /// <returns>True if confirmed, false otherwise</returns>
    Task<bool> ShowConfirmation(string message, string title = "Confirm", string confirmType = "Warning", string confirmText = "Confirm");
}
