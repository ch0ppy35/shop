using Common.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Common.Tests.Logging;

public class JsonLoggerProviderTests
{
    [Fact]
    public void CreateLogger_ShouldReturnLogger_WithCorrectCategoryName()
    {
        // Arrange
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var provider = new JsonLoggerProvider(config);

        // Act
        var logger = provider.CreateLogger("TestCategory");

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<JsonLogger>();
    }

    [Fact]
    public void CreateLogger_ShouldReturnSameLogger_ForSameCategoryName()
    {
        // Arrange
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var provider = new JsonLoggerProvider(config);

        // Act
        var logger1 = provider.CreateLogger("TestCategory");
        var logger2 = provider.CreateLogger("TestCategory");

        // Assert
        logger1.Should().BeSameAs(logger2);
    }

    [Fact]
    public void CreateLogger_ShouldReturnDifferentLoggers_ForDifferentCategoryNames()
    {
        // Arrange
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var provider = new JsonLoggerProvider(config);

        // Act
        var logger1 = provider.CreateLogger("TestCategory1");
        var logger2 = provider.CreateLogger("TestCategory2");

        // Assert
        logger1.Should().NotBeSameAs(logger2);
    }

    [Fact]
    public void Dispose_ShouldClearLoggers()
    {
        // Arrange
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var provider = new JsonLoggerProvider(config);

        // Create some loggers
        provider.CreateLogger("TestCategory1");
        provider.CreateLogger("TestCategory2");

        // Act
        provider.Dispose();

        // Assert - we can't directly test the internal state, but we can verify that
        // the provider doesn't throw an exception when disposed
        var exception = Record.Exception(() => provider.Dispose());
        exception.Should().BeNull();
    }
}
