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

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        if (state is IDictionary<string, object> properties)
        {
            return new JsonLoggerScope(properties);
        }

        return default!;
    }

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
            Exception = exception?.ToString(),
            Properties = JsonLoggerScope.GetCurrentScopeProperties()?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
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
        public Dictionary<string, object>? Properties { get; set; }
    }

    /// <summary>
    /// Scope for the JSON logger
    /// </summary>
    private class JsonLoggerScope : IDisposable
    {
        private static readonly AsyncLocal<Stack<IDictionary<string, object>>> _scopeStack = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLoggerScope"/> class.
        /// </summary>
        public JsonLoggerScope(IDictionary<string, object> properties)
        {
            var stack = _scopeStack.Value;
            if (stack == null)
            {
                stack = new Stack<IDictionary<string, object>>();
                _scopeStack.Value = stack;
            }

            stack.Push(properties);
        }

        /// <summary>
        /// Gets the current scope properties
        /// </summary>
        public static IDictionary<string, object>? GetCurrentScopeProperties()
        {
            var properties = new Dictionary<string, object>();
            var stack = _scopeStack.Value;

            if (stack == null || stack.Count == 0)
            {
                return null;
            }

            foreach (var scope in stack)
            {
                foreach (var kvp in scope)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            return properties;
        }

        /// <summary>
        /// Disposes the scope
        /// </summary>
        public void Dispose()
        {
            var stack = _scopeStack.Value;
            if (stack != null && stack.Count > 0)
            {
                stack.Pop();
            }
        }
    }
}

/// <summary>
/// Configuration for the JSON logger
/// </summary>
public class JsonLoggerConfiguration
{
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
}
