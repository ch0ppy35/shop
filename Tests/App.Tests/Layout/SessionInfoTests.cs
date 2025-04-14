using Bunit;
using FluentAssertions;
using Frontend.Layout;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using App.Tests.TestHelpers;

namespace App.Tests.Layout;

public class SessionInfoTests : TestContext
{
    public SessionInfoTests()
    {
        // Add MudBlazor services
        this.AddMudBlazorTestServices();
    }
    
    [Fact]
    public async Task SessionInfo_ShouldRender_WithSessionId()
    {
        // Arrange
        var sessionId = "test-session-id";
        var mockJsInterop = new MockJavaScriptInterop();

        // Set up the mock to return a session ID
        await mockJsInterop.SetSessionId(sessionId);

        // Register the mock JavaScript interop and session service
        Services.AddSingleton<IJavaScriptInterop>(mockJsInterop);
        Services.AddScoped<SessionService>();

        // Act
        var cut = RenderComponent<SessionInfo>();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains(sessionId));
        
        // MudBlazor uses different markup, so we check for the session ID text instead of exact markup
        cut.Markup.Should().Contain(sessionId);
    }

    [Fact]
    public async Task SessionInfo_ShouldGenerateNewSessionId_WhenNoExistingSessionId()
    {
        // Arrange
        // Create a mock JavaScript interop that we can use to capture the session ID
        var mockJsInterop = new MockJavaScriptInterop();

        // Register the mock JavaScript interop and session service
        Services.AddSingleton<IJavaScriptInterop>(mockJsInterop);
        Services.AddScoped<SessionService>();

        // Act
        var cut = RenderComponent<SessionInfo>();

        // Wait for the component to initialize and generate a session ID
        // This will involve an async call to our mock JavaScript interop
        await Task.Delay(50); // Small delay to ensure async operations complete

        // Assert
        mockJsInterop.CapturedSessionId.Should().NotBeNullOrEmpty();
        
        // The session ID should be a valid GUID
        Guid.TryParse(mockJsInterop.CapturedSessionId, out _).Should().BeTrue();
    }
}
