using System.Diagnostics;

namespace Frontend.Services;

/// <summary>
/// HTTP message handler that adds the session ID to all outgoing requests and times each round trip
/// </summary>
public class SessionHttpMessageHandler : DelegatingHandler
{
    private readonly SessionService _sessionService;
    private readonly RequestTimeline _timeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionHttpMessageHandler"/> class
    /// </summary>
    public SessionHttpMessageHandler(SessionService sessionService, RequestTimeline timeline)
    {
        _sessionService = sessionService;
        _timeline = timeline;
    }

    /// <summary>
    /// Sends the HTTP request with the session ID header
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sessionId = await _sessionService.GetSessionIdAsync();

        request.Headers.Add("X-Session-ID", sessionId);

        var started = Stopwatch.GetTimestamp();
        var status = 0;
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            status = (int)response.StatusCode;
            return response;
        }
        finally
        {
            _timeline.Record(new RequestTiming(
                DateTimeOffset.Now,
                request.Method.Method,
                request.RequestUri?.PathAndQuery ?? "",
                status,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds));
        }
    }
}
