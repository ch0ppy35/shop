using MudBlazor;

namespace Frontend.Services;

/// <summary>
/// Service for displaying toast notifications using MudBlazor Snackbar
/// </summary>
public class ToastService
{
    private readonly ISnackbar _snackbar;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastService"/> class
    /// </summary>
    /// <param name="snackbar">The MudBlazor snackbar service</param>
    public ToastService(ISnackbar snackbar)
    {
        _snackbar = snackbar;
    }

    /// <summary>
    /// Shows a success toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the toast (not used in MudBlazor)</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowSuccess(string message, string title = "Success", int autoHideDelayMs = 3000)
    {
        _snackbar.Add(message, Severity.Success, config => { config.VisibleStateDuration = autoHideDelayMs; });
    }

    /// <summary>
    /// Shows an error toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the toast (not used in MudBlazor)</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowError(string message, string title = "Error", int autoHideDelayMs = 3000)
    {
        _snackbar.Add(message, Severity.Error, config => { config.VisibleStateDuration = autoHideDelayMs; });
    }

    /// <summary>
    /// Shows an info toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the toast (not used in MudBlazor)</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowInfo(string message, string title = "Information", int autoHideDelayMs = 3000)
    {
        _snackbar.Add(message, Severity.Info, config => { config.VisibleStateDuration = autoHideDelayMs; });
    }

    /// <summary>
    /// Shows a warning toast notification
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">The title of the toast (not used in MudBlazor)</param>
    /// <param name="autoHideDelayMs">The delay in milliseconds before auto-hiding the toast</param>
    public void ShowWarning(string message, string title = "Warning", int autoHideDelayMs = 3000)
    {
        _snackbar.Add(message, Severity.Warning, config => { config.VisibleStateDuration = autoHideDelayMs; });
    }
}
