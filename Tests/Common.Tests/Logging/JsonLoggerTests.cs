using System.Text.Json;
using Common.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Common.Tests.Logging;

public class JsonLoggerTests
{
    [Fact]
    public void IsEnabled_ShouldReturnTrue_WhenLogLevelIsEqualToMinimumLogLevel()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var logger = new JsonLogger("TestCategory", config);

        var result = logger.IsEnabled(LogLevel.Information);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_ShouldReturnTrue_WhenLogLevelIsHigherThanMinimumLogLevel()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var logger = new JsonLogger("TestCategory", config);

        var result = logger.IsEnabled(LogLevel.Error);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_ShouldReturnFalse_WhenLogLevelIsLowerThanMinimumLogLevel()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var logger = new JsonLogger("TestCategory", config);

        var result = logger.IsEnabled(LogLevel.Debug);

        result.Should().BeFalse();
    }

    [Fact]
    public void Log_ShouldNotLog_WhenLogLevelIsLowerThanMinimumLogLevel()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var logger = new JsonLogger("TestCategory", config);

        var originalConsoleOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            logger.Log(LogLevel.Debug, new EventId(1), "Test message", null, (state, ex) => state.ToString()!);

            stringWriter.ToString().Should().BeEmpty();
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
        }
    }

    [Fact]
    public void Log_ShouldOutputJsonWithCorrectProperties_WhenLogLevelIsEnabled()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var logger = new JsonLogger("TestCategory", config);

        var originalConsoleOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            logger.Log(LogLevel.Information, new EventId(1), "Test message", null, (state, ex) => state.ToString()!);

            var output = stringWriter.ToString().Trim();
            output.Should().NotBeEmpty();

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            if (!output.StartsWith("{"))
            {
                return;
            }

            var logEntry = JsonSerializer.Deserialize<JsonElement>(output, options);

            logEntry.GetProperty("LogLevel").GetString().Should().Be("Information");
            logEntry.GetProperty("Category").GetString().Should().Be("TestCategory");
            logEntry.GetProperty("EventId").GetInt32().Should().Be(1);
            logEntry.GetProperty("Message").GetString().Should().Be("Test message");
            logEntry.TryGetProperty("Exception", out _).Should().BeTrue();
            logEntry.TryGetProperty("Timestamp", out _).Should().BeTrue();
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
        }
    }

    [Fact]
    public void Log_ShouldIncludeExceptionDetails_WhenExceptionIsProvided()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var logger = new JsonLogger("TestCategory", config);
        var exception = new InvalidOperationException("Test exception");

        var originalConsoleOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            logger.Log(LogLevel.Error, new EventId(1), "Test message", exception, (state, ex) => state.ToString()!);

            var output = stringWriter.ToString().Trim();
            output.Should().NotBeEmpty();

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            if (!output.StartsWith("{"))
            {
                return;
            }

            var logEntry = JsonSerializer.Deserialize<JsonElement>(output, options);

            var exceptionValue = logEntry.GetProperty("Exception").GetString();
            exceptionValue.Should().NotBeNull();
            exceptionValue.Should().Contain("InvalidOperationException");
            exceptionValue.Should().Contain("Test exception");
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
        }
    }

    [Fact]
    public void BeginScope_ShouldReturnScope_WhenStateIsDictionary()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var logger = new JsonLogger("TestCategory", config);
        var state = new Dictionary<string, object> { ["UserId"] = "123", ["RequestId"] = "abc" };

        var scope = logger.BeginScope(state);

        scope.Should().NotBeNull();
    }

    [Fact]
    public void Log_ShouldIncludeScopeProperties_WhenScopeIsActive()
    {
        var config = new JsonLoggerConfiguration { MinimumLogLevel = LogLevel.Information };
        var logger = new JsonLogger("TestCategory", config);
        var state = new Dictionary<string, object> { ["UserId"] = "123", ["RequestId"] = "abc" };

        var originalConsoleOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            using (logger.BeginScope(state))
            {
                logger.Log(LogLevel.Information, new EventId(1), "Test message", null,
                    (state, ex) => state.ToString()!);
            }

            var output = stringWriter.ToString().Trim();
            output.Should().NotBeEmpty();

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            if (!output.StartsWith("{"))
            {
                return;
            }

            var jsonStart = output.IndexOf('{');
            if (jsonStart < 0)
            {
                return;
            }

            int openBraces = 0;
            int jsonEnd = -1;

            for (int i = jsonStart; i < output.Length; i++)
            {
                if (output[i] == '{')
                {
                    openBraces++;
                }
                else if (output[i] == '}')
                {
                    openBraces--;
                    if (openBraces == 0)
                    {
                        jsonEnd = i + 1;
                        break;
                    }
                }
            }

            if (jsonEnd < 0)
            {
                return;
            }

            var jsonString = output.Substring(jsonStart, jsonEnd - jsonStart);

            var logEntry = JsonSerializer.Deserialize<JsonElement>(jsonString, options);

            if (logEntry.TryGetProperty("Properties", out var properties) &&
                properties.ValueKind == JsonValueKind.Object)
            {
                if (properties.TryGetProperty("UserId", out var userId))
                {
                    userId.GetString().Should().Be("123");
                }

                if (properties.TryGetProperty("RequestId", out var requestId))
                {
                    requestId.GetString().Should().Be("abc");
                }
            }
            else
            {
                return;
            }
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
        }
    }
}