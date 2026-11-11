using FluentAssertions;
using Gateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gateway.Tests;

public class SessionMiddlewareTests
{
    private static async Task<string> RunAsync(Action<HttpContext> arrange)
    {
        var context = new DefaultHttpContext();
        arrange(context);
        var middleware = new SessionMiddleware(_ => Task.CompletedTask, NullLogger<SessionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var sessionId = (string)context.Items["SessionId"]!;
        context.Response.Headers[SessionMiddleware.HeaderName].ToString().Should().Be(sessionId);
        return sessionId;
    }

    [Fact]
    public async Task GuidHeader_ShouldBeKept_InNormalizedForm()
    {
        var id = Guid.NewGuid();

        var sessionId = await RunAsync(c => c.Request.Headers[SessionMiddleware.HeaderName] = id.ToString("D").ToUpperInvariant());

        sessionId.Should().Be(id.ToString("D"));
    }

    [Theory]
    [InlineData("evil.>")]
    [InlineData("*")]
    [InlineData("not a guid")]
    public async Task NonGuidHeader_ShouldBeReplaced_SoItCanNeverReachANatsSubject(string header)
    {
        var sessionId = await RunAsync(c => c.Request.Headers[SessionMiddleware.HeaderName] = header);

        Guid.TryParse(sessionId, out _).Should().BeTrue();
        sessionId.Should().NotBe(header);
    }

    [Fact]
    public async Task SessionQueryParameter_ShouldBeUsed_WhenThereIsNoHeader()
    {
        var id = Guid.NewGuid().ToString("D");

        var sessionId = await RunAsync(c => c.Request.QueryString = new QueryString($"?session={id}"));

        sessionId.Should().Be(id);
    }
}
