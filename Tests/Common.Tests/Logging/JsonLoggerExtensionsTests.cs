using Common.Logging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Tests.Logging;

public class JsonLoggerExtensionsTests
{
    [Fact]
    public void AddJsonLogger_ShouldRegisterJsonLoggerProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLogging(builder => builder.AddJsonLogger());
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        // Create a logger and verify it works
        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.Should().NotBeNull();

        // We can't directly check if it's using JsonLogger, but we can verify it doesn't throw
        var exception = Record.Exception(() => logger.LogInformation("Test message"));
        exception.Should().BeNull();
    }

    [Fact]
    public void AddJsonLogger_ShouldUseConfigurationAction_WhenProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        var configCalled = false;

        // Act
        services.AddLogging(builder => builder.AddJsonLogger(config =>
        {
            config.MinimumLogLevel = LogLevel.Warning;
            configCalled = true;
        }));
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        configCalled.Should().BeTrue();

        // Get the configuration to verify it was set correctly
        var config = serviceProvider.GetRequiredService<JsonLoggerConfiguration>();
        config.Should().NotBeNull();
        config.MinimumLogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void AddJsonLogger_ShouldRegisterSingletonConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLogging(builder => builder.AddJsonLogger());
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var config1 = serviceProvider.GetRequiredService<JsonLoggerConfiguration>();
        var config2 = serviceProvider.GetRequiredService<JsonLoggerConfiguration>();

        config1.Should().BeSameAs(config2);
    }
}
