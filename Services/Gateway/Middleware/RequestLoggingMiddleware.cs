using System.Diagnostics;

namespace Gateway.Middleware;

/// <summary>
/// Middleware to log request details including User-Agent
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
    /// </summary>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Start timing the request
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Extract request details
            var userAgent = context.Request.Headers.UserAgent.ToString() ?? "Unknown";
            var method = context.Request.Method;
            var path = context.Request.Path;
            var queryString = context.Request.QueryString;
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var requestId = Guid.NewGuid().ToString();
            var sessionId = context.Items["SessionId"]?.ToString() ?? "Unknown";

            // Create a scope with request details
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["SessionId"] = sessionId,
                ["UserAgent"] = userAgent,
                ["ClientIP"] = clientIp,
                ["Method"] = method,
                ["Path"] = path.ToString(),
                ["QueryString"] = queryString.ToString()
            }))
            {
                // Log the incoming request
                _logger.LogInformation(
                    "Request: {Method} {Path}{QueryString} - ClientIP: {ClientIP} - UserAgent: {UserAgent} - SessionId: {SessionId}",
                    method, path, queryString, clientIp, userAgent, sessionId);

                // Call the next middleware in the pipeline
                await _next(context);

                // Log the response
                stopwatch.Stop();
                _logger.LogInformation(
                    "Response: {StatusCode} for {Method} {Path} - Completed in {ElapsedMilliseconds}ms",
                    context.Response.StatusCode, method, path, stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Error processing request: {Method} {Path} - Error: {ErrorMessage} - Completed in {ElapsedMilliseconds}ms",
                context.Request.Method, context.Request.Path, ex.Message, stopwatch.ElapsedMilliseconds);

            // Re-throw the exception to be handled by the error handling middleware
            throw;
        }
    }
}
