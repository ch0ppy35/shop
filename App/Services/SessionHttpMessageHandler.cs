using System.Diagnostics;

namespace Frontend.Services;

/// <summary>
/// HTTP message handler that adds the session ID to requests that need one and times each round trip
/// </summary>
/// <remarks>
/// A custom header turns a cross-origin GET into a "non-simple" request that the browser has to preflight with
/// an extra OPTIONS round trip. Catalog and recommendation reads don't depend on the session, so they go
/// without it and reach the gateway in a single round trip.
/// </remarks>
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
        if (NeedsSession(request))
        {
            request.Headers.Add("X-Session-ID", await _sessionService.GetSessionIdAsync());
        }

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

    /// <summary>
    /// Gets whether a request depends on the shopper's session: everything under /api/cart, and every write
    /// </summary>
    public static bool NeedsSession(HttpRequestMessage request) =>
        request.Method != HttpMethod.Get ||
        (request.RequestUri?.IsAbsoluteUri == true ? request.RequestUri.AbsolutePath : request.RequestUri?.OriginalString ?? "")
        .StartsWith("/api/cart", StringComparison.OrdinalIgnoreCase);
}
