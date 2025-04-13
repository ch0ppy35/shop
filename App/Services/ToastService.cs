namespace Frontend.Services;

/// <summary>
/// Service for displaying toast notifications
/// </summary>
public class ToastService
{
    /// <summary>
    /// Event raised when a toast should be shown
    /// </summary>
    public event Action<string, string, string, int>? OnShow;

    /// <summary>
    /// Shows a success toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the toast</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowSuccess(string message, string title = "Success", int autoHideDelayMs = 3000)
    {
        OnShow?.Invoke(message, title, "Success", autoHideDelayMs);
    }

    /// <summary>
    /// Shows an error toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the toast</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowError(string message, string title = "Error", int autoHideDelayMs = 3000)
    {
        OnShow?.Invoke(message, title, "Error", autoHideDelayMs);
    }

    /// <summary>
    /// Shows an info toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the toast</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowInfo(string message, string title = "Information", int autoHideDelayMs = 3000)
    {
        OnShow?.Invoke(message, title, "Info", autoHideDelayMs);
    }

    /// <summary>
    /// Shows a warning toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the toast</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowWarning(string message, string title = "Warning", int autoHideDelayMs = 3000)
    {
        OnShow?.Invoke(message, title, "Warning", autoHideDelayMs);
    }
}
