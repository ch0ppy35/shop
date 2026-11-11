namespace Gateway.Middleware;

/// <summary>
/// Middleware to handle session IDs
/// </summary>
/// <remarks>
/// Session IDs end up in Valkey keys, NATS subjects (<c>cart.changed.{session}</c>) and SignalR group names, so only
/// a GUID is accepted, normalized to its lowercase "D" form. Anything else gets a fresh session. The SignalR hub
/// can't send headers over a browser WebSocket, so it may pass the session as the <c>session</c> query parameter.
/// </remarks>
public class SessionMiddleware
{
    /// <summary>
    /// The header carrying the session ID
    /// </summary>
    public const string HeaderName = "X-Session-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<SessionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionMiddleware"/> class.
    /// </summary>
    public SessionMiddleware(RequestDelegate next, ILogger<SessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Reads a session ID if it is a GUID, normalized; otherwise null
    /// </summary>
    public static string? Normalize(string? value) =>
        Guid.TryParse(value, out var guid) ? guid.ToString("D") : null;

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var sessionId = Normalize(context.Request.Headers[HeaderName])
                        ?? Normalize(context.Request.Query["session"]);

        if (sessionId == null)
        {
            sessionId = Guid.NewGuid().ToString("D");
            _logger.LogInformation("Request had no valid session ID; issued {SessionId}", sessionId);
        }

        context.Request.Headers[HeaderName] = sessionId;
        context.Items["SessionId"] = sessionId;
        context.Response.Headers[HeaderName] = sessionId;

        await _next(context);
    }
}

/// <summary>
/// Extension methods for the SessionMiddleware
/// </summary>
public static class SessionMiddlewareExtensions
{
    /// <summary>
    /// Adds the session middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseSessionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionMiddleware>();
    }
}
