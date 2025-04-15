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
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddJsonLogger());
        var serviceProvider = services.BuildServiceProvider();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.Should().NotBeNull();

        var exception = Record.Exception(() => logger.LogInformation("Test message"));
        exception.Should().BeNull();
    }

    [Fact]
    public void AddJsonLogger_ShouldUseConfigurationAction_WhenProvided()
    {
        var services = new ServiceCollection();
        var configCalled = false;

        services.AddLogging(builder => builder.AddJsonLogger(config =>
        {
            config.MinimumLogLevel = LogLevel.Warning;
            configCalled = true;
        }));
        var serviceProvider = services.BuildServiceProvider();

        configCalled.Should().BeTrue();

        var config = serviceProvider.GetRequiredService<JsonLoggerConfiguration>();
        config.Should().NotBeNull();
        config.MinimumLogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void AddJsonLogger_ShouldRegisterSingletonConfiguration()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddJsonLogger());
        var serviceProvider = services.BuildServiceProvider();

        var config1 = serviceProvider.GetRequiredService<JsonLoggerConfiguration>();
        var config2 = serviceProvider.GetRequiredService<JsonLoggerConfiguration>();

        config1.Should().BeSameAs(config2);
    }
}
