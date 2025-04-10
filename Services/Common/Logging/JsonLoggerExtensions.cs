using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Logging;

/// <summary>
/// Extension methods for adding the JSON logger to the logging builder
/// </summary>
public static class JsonLoggerExtensions
{
    /// <summary>
    /// Adds the JSON logger to the logging builder
    /// </summary>
    public static ILoggingBuilder AddJsonLogger(this ILoggingBuilder builder, Action<JsonLoggerConfiguration>? configure = null)
    {
        var config = new JsonLoggerConfiguration();
        configure?.Invoke(config);

        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton<ILoggerProvider, JsonLoggerProvider>();

        return builder;
    }
}
