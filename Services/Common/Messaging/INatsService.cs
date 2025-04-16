using Common.Models;

namespace Common.Messaging;

/// <summary>
/// Interface for interacting with NATS
/// </summary>
public interface INatsService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the service is connected to NATS
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the NATS server
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the NATS server with retry mechanism
    /// </summary>
    Task ConnectWithRetryAsync(int maxRetries = -1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to the specified subject
    /// </summary>
    Task PublishAsync<T>(string subject, T message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request message and waits for a reply
    /// </summary>
    Task<TResponse?> RequestAsync<TRequest, TResponse>(string subject, TRequest message, TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Logs information about a queue group subscription (for testing)
    /// </summary>
    void LogQueueGroupInfo(string subject, string queueGroup);

    /// <summary>
    /// Subscribes to the specified subject
    /// </summary>
    IAsyncEnumerable<T> SubscribeAsync<T>(string subject, string? queueGroup = null,
        CancellationToken cancellationToken = default)
        where T : BaseMessage;
}