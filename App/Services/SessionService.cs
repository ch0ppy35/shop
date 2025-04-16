namespace Frontend.Services;

/// <summary>
/// Service for managing session IDs
/// </summary>
public class SessionService
{
    private readonly IJavaScriptInterop _jsInterop;
    private string? _sessionId;
    private const string SessionIdStorageKey = "nats_shop_session_id";

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionService"/> class
    /// </summary>
    public SessionService(IJavaScriptInterop jsInterop)
    {
        _jsInterop = jsInterop;
    }

    /// <summary>
    /// Gets the current session ID, generating a new one if needed
    /// </summary>
    public async Task<string> GetSessionIdAsync()
    {
        if (!string.IsNullOrEmpty(_sessionId))
        {
            return _sessionId;
        }

        try
        {
            var storedSessionId = await _jsInterop.GetSessionId();

            if (!string.IsNullOrEmpty(storedSessionId))
            {
                _sessionId = storedSessionId;
                return _sessionId;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving session ID from local storage: {ex.Message}");
        }

        _sessionId = Guid.NewGuid().ToString();

        try
        {
            await _jsInterop.SetSessionId(_sessionId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing session ID in local storage: {ex.Message}");
        }

        return _sessionId;
    }
}