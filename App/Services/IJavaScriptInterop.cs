namespace Frontend.Services;

/// <summary>
/// Interface for JavaScript interop operations
/// </summary>
public interface IJavaScriptInterop
{
    /// <summary>
    /// Gets the session ID from local storage
    /// </summary>
    Task<string?> GetSessionId();

    /// <summary>
    /// Sets the session ID in local storage
    /// </summary>
    Task SetSessionId(string sessionId);
}
