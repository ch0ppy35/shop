using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Integration.Tests.Fixtures;

/// <summary>
/// A testable version of NatsService that adds handler registration for testing
/// </summary>
public class TestableNatsService : NatsService
{
    private readonly Dictionary<string, Func<string, Task<string>>> _handlers = new();
    private readonly ILogger<NatsService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestableNatsService"/> class.
    /// </summary>
    public TestableNatsService(ILogger<NatsService> logger, IConfiguration configuration)
        : base(logger, configuration)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a handler for a subject
    /// </summary>
    public void RegisterHandler(string subject, Func<string, Task<string>> handler)
    {
        _handlers[subject] = handler;
    }

    /// <summary>
    /// Sends a request message and waits for a reply
    /// </summary>
    public new async Task<TResponse?> RequestAsync<TRequest, TResponse>(string subject, TRequest message,
        TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        // Set a default timeout to prevent tests from hanging
        timeout ??= TimeSpan.FromSeconds(5);

        // Create a cancellation token with the timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout.Value);

        Console.WriteLine($"TestableNatsService: Sending request to subject: {subject}");

        // If we have a registered handler, use it
        if (_handlers.TryGetValue(subject, out var handler))
        {
            try
            {
                _logger.LogDebug("Using registered handler for subject: {Subject}", subject);
                var messageJson = System.Text.Json.JsonSerializer.Serialize(message);
                Console.WriteLine($"TestableNatsService: Invoking handler for subject: {subject}");
                var responseJson = await handler(messageJson);
                Console.WriteLine($"TestableNatsService: Handler for subject {subject} returned response");
                return System.Text.Json.JsonSerializer.Deserialize<TResponse>(responseJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestableNatsService: Error using handler for subject {subject}: {ex.Message}");
                _logger.LogError(ex, "Error using registered handler for subject {Subject}: {Message}", subject, ex.Message);
                throw;
            }
        }

        Console.WriteLine($"TestableNatsService: No handler registered for subject: {subject}, using base implementation");
        // Otherwise, use the base implementation
        return await base.RequestAsync<TRequest, TResponse>(subject, message, timeout, cts.Token);
    }
}
