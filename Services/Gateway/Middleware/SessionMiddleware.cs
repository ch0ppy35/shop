namespace Gateway.Middleware;

/// <summary>
/// Middleware to handle session IDs
/// </summary>
public class SessionMiddleware
{
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
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Session-ID", out var sessionId) || string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            context.Request.Headers["X-Session-ID"] = sessionId;
        }

        context.Items["SessionId"] = sessionId.ToString();

        _logger.LogInformation("Request associated with session ID: {SessionId}", sessionId.ToString());

        if (!context.Response.Headers.ContainsKey("X-Session-ID"))
        {
            context.Response.Headers["X-Session-ID"] = sessionId.ToString();
        }

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
