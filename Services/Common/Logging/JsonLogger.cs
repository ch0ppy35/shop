using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Common.Logging;

/// <summary>
/// JSON logger that outputs log messages in JSON format
/// </summary>
public class JsonLogger : ILogger
{
    private readonly string _categoryName;
    private readonly JsonLoggerConfiguration _config;

    public JsonLogger(string categoryName, JsonLoggerConfiguration config)
    {
        _categoryName = categoryName;
        _config = config;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _config.MinimumLogLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel.ToString(),
            Category = _categoryName,
            EventId = eventId.Id,
            Message = formatter(state, exception),
            Exception = exception?.ToString()
        };

        var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        Console.WriteLine(json);
    }

    private class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string? LogLevel { get; set; }
        public string? Category { get; set; }
        public int EventId { get; set; }
        public string? Message { get; set; }
        public string? Exception { get; set; }
    }
}

/// <summary>
/// Configuration for the JSON logger
/// </summary>
public class JsonLoggerConfiguration
{
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
}
