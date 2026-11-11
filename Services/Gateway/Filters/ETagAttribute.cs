using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Gateway.Filters;

/// <summary>
/// Adds an ETag to successful GET responses and answers 304 Not Modified when the browser already has them
/// </summary>
/// <remarks>
/// Uses <c>Cache-Control: no-cache</c>, not a max-age: the browser keeps the body but checks with the gateway
/// every time. That keeps live updates correct (after a <c>ProductChanged</c> push a refetch must not be served
/// from a stale browser cache) while unchanged data costs a bodiless 304.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ETagAttribute : Attribute, IAsyncResultFilter
{
    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var http = context.HttpContext;

        if (HttpMethods.IsGet(http.Request.Method) &&
            context.Result is ObjectResult { Value: not null } result &&
            (result.StatusCode ?? StatusCodes.Status200OK) == StatusCodes.Status200OK)
        {
            var jsonOptions = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
            var body = JsonSerializer.SerializeToUtf8Bytes(result.Value, result.Value.GetType(),
                jsonOptions.JsonSerializerOptions);
            var etag = $"\"{Convert.ToHexString(SHA256.HashData(body), 0, 16)}\"";

            http.Response.Headers.ETag = etag;
            http.Response.Headers.CacheControl = "no-cache";

            context.Result = http.Request.Headers.IfNoneMatch.Contains(etag)
                ? new StatusCodeResult(StatusCodes.Status304NotModified)
                : new FileContentResult(body, "application/json; charset=utf-8");
        }

        await next();
    }
}
