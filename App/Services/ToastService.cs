namespace Frontend.Services;

/// <summary>
/// The visual style of a toast
/// </summary>
public enum ToastKind
{
    Success,
    Error,
    Info,
    Warning
}

/// <summary>
/// A toast notification currently on screen
/// </summary>
public sealed record Toast(Guid Id, ToastKind Kind, string Message);

/// <summary>
/// Service for displaying toast notifications rendered by the Toaster component
/// </summary>
public class ToastService
{
    private readonly List<Toast> _toasts = new();

    /// <summary>
    /// Raised whenever a toast is shown or dismissed
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the toasts currently on screen, oldest first
    /// </summary>
    public IReadOnlyList<Toast> Toasts => _toasts;

    /// <summary>
    /// Shows a success toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">Unused; kept for call-site compatibility</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowSuccess(string message, string title = "Success", int autoHideDelayMs = 3000) =>
        Show(ToastKind.Success, message, autoHideDelayMs);

    /// <summary>
    /// Shows an error toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">Unused; kept for call-site compatibility</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowError(string message, string title = "Error", int autoHideDelayMs = 5000) =>
        Show(ToastKind.Error, message, autoHideDelayMs);

    /// <summary>
    /// Shows an info toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">Unused; kept for call-site compatibility</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowInfo(string message, string title = "Information", int autoHideDelayMs = 3000) =>
        Show(ToastKind.Info, message, autoHideDelayMs);

    /// <summary>
    /// Shows a warning toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">Unused; kept for call-site compatibility</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowWarning(string message, string title = "Warning", int autoHideDelayMs = 4000) =>
        Show(ToastKind.Warning, message, autoHideDelayMs);

    /// <summary>
    /// Removes a toast before its timer runs out
    /// </summary>
    /// <param name="id">The toast ID</param>
    public void Dismiss(Guid id)
    {
        if (_toasts.RemoveAll(t => t.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }

    private void Show(ToastKind kind, string message, int autoHideDelayMs)
    {
        var toast = new Toast(Guid.NewGuid(), kind, message);
        _toasts.Add(toast);
        Changed?.Invoke();

        if (autoHideDelayMs > 0)
        {
            _ = DismissLaterAsync(toast.Id, autoHideDelayMs);
        }
    }

    private async Task DismissLaterAsync(Guid id, int delayMs)
    {
        await Task.Delay(delayMs);
        Dismiss(id);
    }
}
