using Microsoft.Extensions.Logging;

namespace Common.Logging;

/// <summary>
/// Provider for the JSON logger
/// </summary>
[ProviderAlias("JsonLogger")]
public class JsonLoggerProvider : ILoggerProvider
{
    private readonly JsonLoggerConfiguration _config;
    private readonly Dictionary<string, JsonLogger> _loggers = new();

    public JsonLoggerProvider(JsonLoggerConfiguration config)
    {
        _config = config;
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (!_loggers.TryGetValue(categoryName, out var logger))
        {
            logger = new JsonLogger(categoryName, _config);
            _loggers[categoryName] = logger;
        }

        return logger;
    }

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
