using Frontend.Services;

namespace App.Tests.TestHelpers;

/// <summary>
/// Mock implementation of IJavaScriptInterop for testing
/// </summary>
public class MockJavaScriptInterop : IJavaScriptInterop
{
    private string? _sessionId;
    
    /// <summary>
    /// Gets the captured session ID that was set
    /// </summary>
    public string? CapturedSessionId => _sessionId;
    
    /// <summary>
    /// Gets the session ID from "local storage"
    /// </summary>
    public Task<string?> GetSessionId()
    {
        return Task.FromResult(_sessionId);
    }

    /// <summary>
    /// Sets the session ID in "local storage"
    /// </summary>
    public Task SetSessionId(string sessionId)
    {
        _sessionId = sessionId;
        return Task.CompletedTask;
    }
}
