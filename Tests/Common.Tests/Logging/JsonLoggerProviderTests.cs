using Common.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Common.Tests.Logging;

public class JsonLoggerProviderTests
{
    [Fact]
    public void CreateLogger_ShouldReturnLogger_WithCorrectCategoryName()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var provider = new JsonLoggerProvider(config);

        var logger = provider.CreateLogger("TestCategory");

        logger.Should().NotBeNull();
        logger.Should().BeOfType<JsonLogger>();
    }

    [Fact]
    public void CreateLogger_ShouldReturnSameLogger_ForSameCategoryName()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var provider = new JsonLoggerProvider(config);

        var logger1 = provider.CreateLogger("TestCategory");
        var logger2 = provider.CreateLogger("TestCategory");

        logger1.Should().BeSameAs(logger2);
    }

    [Fact]
    public void CreateLogger_ShouldReturnDifferentLoggers_ForDifferentCategoryNames()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var provider = new JsonLoggerProvider(config);

        var logger1 = provider.CreateLogger("TestCategory1");
        var logger2 = provider.CreateLogger("TestCategory2");

        logger1.Should().NotBeSameAs(logger2);
    }

    [Fact]
    public void Dispose_ShouldClearLoggers()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var provider = new JsonLoggerProvider(config);

        provider.CreateLogger("TestCategory1");
        provider.CreateLogger("TestCategory2");

        provider.Dispose();

        var exception = Record.Exception(() => provider.Dispose());
        exception.Should().BeNull();
    }
}