namespace Frontend.Services;

/// <summary>
/// HTTP message handler that adds the session ID to all outgoing requests
/// </summary>
public class SessionHttpMessageHandler : DelegatingHandler
{
    private readonly SessionService _sessionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionHttpMessageHandler"/> class
    /// </summary>
    public SessionHttpMessageHandler(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// Sends the HTTP request with the session ID header
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the session ID
        var sessionId = await _sessionService.GetSessionIdAsync();

        // Add the session ID to the request headers
        request.Headers.Add("X-Session-ID", sessionId);

        // Call the inner handler
        return await base.SendAsync(request, cancellationToken);
    }
}
