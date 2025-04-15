using Microsoft.JSInterop;

namespace Frontend.Services;

/// <summary>
/// Implementation of IJavaScriptInterop using IJSRuntime
/// </summary>
public class JavaScriptInterop : IJavaScriptInterop
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptInterop"/> class
    /// </summary>
    public JavaScriptInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets the session ID from local storage
    /// </summary>
    public async Task<string?> GetSessionId()
    {
        return await _jsRuntime.InvokeAsync<string?>("configHelper.session.getSessionId");
    }

    /// <summary>
    /// Sets the session ID in local storage
    /// </summary>
    public async Task SetSessionId(string sessionId)
    {
        await _jsRuntime.InvokeVoidAsync("configHelper.session.setSessionId", sessionId);
    }
}