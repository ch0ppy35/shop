using System.Net;
using FluentAssertions;
using Frontend.Services;
using Moq;
using Moq.Protected;

namespace App.Tests.Services;

public class RequestTimelineTests
{
    private static RequestTiming Timing(string path) =>
        new(DateTimeOffset.Now, "GET", path, 200, 1.5);

    [Fact]
    public void Record_ShouldKeepNewestFirst()
    {
        var timeline = new RequestTimeline();

        timeline.Record(Timing("/a"));
        timeline.Record(Timing("/b"));

        timeline.Entries.Select(e => e.Path).Should().Equal("/b", "/a");
        timeline.Latest!.Path.Should().Be("/b");
    }

    [Fact]
    public void Record_ShouldDropOldest_PastCapacity()
    {
        var timeline = new RequestTimeline();

        for (var i = 0; i < 30; i++)
        {
            timeline.Record(Timing($"/{i}"));
        }

        timeline.Entries.Should().HaveCount(25);
        timeline.Entries.Last().Path.Should().Be("/5");
    }

    [Fact]
    public async Task SessionHttpMessageHandler_ShouldRecordEachRequest()
    {
        var timeline = new RequestTimeline();
        var sessionService = new SessionService(new TestHelpers.MockJavaScriptInterop());
        var inner = new Mock<HttpMessageHandler>();
        inner.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var handler = new SessionHttpMessageHandler(sessionService, timeline) { InnerHandler = inner.Object };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        await client.GetAsync("/api/products?page=2");

        timeline.Latest.Should().NotBeNull();
        timeline.Latest!.Method.Should().Be("GET");
        timeline.Latest.Path.Should().Be("/api/products?page=2");
        timeline.Latest.Status.Should().Be(404);
        timeline.Latest.ElapsedMs.Should().BeGreaterThanOrEqualTo(0);
    }
}
