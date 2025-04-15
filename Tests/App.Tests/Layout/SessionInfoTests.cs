using App.Tests.TestHelpers;
using Bunit;
using FluentAssertions;
using Frontend.Layout;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;

namespace App.Tests.Layout;

public class SessionInfoTests : TestContext
{
    public SessionInfoTests()
    {
        this.AddMudBlazorTestServices();
    }

    [Fact]
    public async Task SessionInfo_ShouldRender_WithSessionId()
    {
        var sessionId = "test-session-id";
        var mockJsInterop = new MockJavaScriptInterop();

        await mockJsInterop.SetSessionId(sessionId);

        Services.AddSingleton<IJavaScriptInterop>(mockJsInterop);
        Services.AddScoped<SessionService>();

        var cut = RenderComponent<SessionInfo>();

        cut.WaitForState(() => cut.Markup.Contains(sessionId));

        cut.Markup.Should().Contain(sessionId);
    }

    [Fact]
    public async Task SessionInfo_ShouldGenerateNewSessionId_WhenNoExistingSessionId()
    {
        var mockJsInterop = new MockJavaScriptInterop();

        Services.AddSingleton<IJavaScriptInterop>(mockJsInterop);
        Services.AddScoped<SessionService>();

        var cut = RenderComponent<SessionInfo>();

        await Task.Delay(50); // Small delay to ensure async operations complete

        mockJsInterop.CapturedSessionId.Should().NotBeNullOrEmpty();

        Guid.TryParse(mockJsInterop.CapturedSessionId, out _).Should().BeTrue();
    }
}