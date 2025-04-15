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
        var sessionId = await _sessionService.GetSessionIdAsync();

        request.Headers.Add("X-Session-ID", sessionId);

        return await base.SendAsync(request, cancellationToken);
    }
}
