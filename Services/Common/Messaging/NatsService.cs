using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using Common.Models;

namespace Common.Messaging;

/// <summary>
/// Service for interacting with NATS
/// </summary>
public class NatsService : IAsyncDisposable
{
    private readonly ILogger<NatsService> _logger;
    private readonly NatsOpts _natsOpts;
    private NatsConnection? _connection;
    private bool _isConnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsService"/> class.
    /// </summary>
    public NatsService(ILogger<NatsService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get NATS configuration from environment variables or configuration
        var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ??
                     configuration.GetValue<string>("Nats:Url") ??
                     "nats://localhost:4222";

        _natsOpts = new NatsOpts
        {
            Url = natsUrl,
            Name = "NatsShop.Service",
            ReconnectWaitMax = TimeSpan.FromSeconds(10),
            ConnectTimeout = TimeSpan.FromSeconds(5),
            SerializerRegistry = NatsJsonSerializerRegistry.Default,
        };

        _logger.LogInformation("NATS configuration: URL={NatsUrl}", natsUrl);
    }

    /// <summary>
    /// Gets a value indicating whether the service is connected to NATS
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Connects to the NATS server
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await ConnectWithRetryAsync(1, cancellationToken);
    }

    /// <summary>
    /// Connects to the NATS server with retry mechanism
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries, use -1 for infinite retries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ConnectWithRetryAsync(int maxRetries = -1, CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        const int retryDelaySeconds = 10;

        while (!_isConnected && (maxRetries == -1 || retryCount <= maxRetries))
        {
            try
            {
                if (retryCount > 0)
                {
                    _logger.LogInformation("Retry {RetryCount}: Connecting to NATS server at {Url}",
                        retryCount, _natsOpts.Url);
                }
                else
                {
                    _logger.LogInformation("Connecting to NATS server at {Url}", _natsOpts.Url);
                }

                // Dispose previous connection if it exists
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                }

                _connection = new NatsConnection(_natsOpts);
                await _connection.ConnectAsync();

                _isConnected = true;
                _logger.LogInformation("Successfully connected to NATS server");
                return;
            }
            catch (Exception ex)
            {
                _isConnected = false;

                if (maxRetries == -1 || retryCount < maxRetries)
                {
                    _logger.LogWarning(ex, "Failed to connect to NATS server: {Message}. Retrying in {RetryDelay} seconds...",
                        ex.Message, retryDelaySeconds);

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("NATS connection retry cancelled");
                        throw;
                    }
                }
                else
                {
                    _logger.LogError(ex, "Failed to connect to NATS server after {RetryCount} retries: {Message}",
                        retryCount, ex.Message);
                    throw;
                }
            }

            retryCount++;
        }
    }

    /// <summary>
    /// Publishes a message to the specified subject
    /// </summary>
    public async Task PublishAsync<T>(string subject, T message, CancellationToken cancellationToken = default)
    {
        if (_connection == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected to NATS server");
        }

        try
        {
            _logger.LogDebug("Publishing message to subject: {Subject}", subject);
            await _connection.PublishAsync(subject, message, cancellationToken: cancellationToken);
            _logger.LogDebug("Message published to subject: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to subject {Subject}: {Message}", subject, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Sends a request message and waits for a reply
    /// </summary>
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(string subject, TRequest message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        if (_connection == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected to NATS server");
        }

        try
        {
            _logger.LogDebug("Sending request to subject: {Subject}", subject);

            // Use default timeout of 10 seconds if not specified
            timeout ??= TimeSpan.FromSeconds(10);

            // Create a cancellation token source with the timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout.Value);

            // Use the built-in request-reply functionality
            var reply = await _connection.RequestAsync<TRequest, TResponse>(subject, message, cancellationToken: cts.Token);

            _logger.LogDebug("Received reply from subject: {Subject}", subject);
            return reply.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request to subject {Subject}: {Message}", subject, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Subscribes to the specified subject
    /// </summary>
    /// <param name="subject">The subject to subscribe to</param>
    /// <param name="queueGroup">Optional queue group name for load balancing across multiple subscribers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of messages</returns>
    public async IAsyncEnumerable<T> SubscribeAsync<T>(string subject, string? queueGroup = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : BaseMessage
    {
        if (_connection == null || !_isConnected)
        {
            throw new InvalidOperationException("Not connected to NATS server");
        }

        _logger.LogInformation("Subscribing to subject: {Subject} with queue group: {QueueGroup}",
            subject, queueGroup ?? "none");

        // Create subscription with or without queue group
        IAsyncEnumerable<NatsMsg<T>> asyncEnumerable;

        if (string.IsNullOrEmpty(queueGroup))
        {
            // Standard subscription without queue group
            asyncEnumerable = _connection.SubscribeAsync<T>(subject, cancellationToken: cancellationToken);
        }
        else
        {
            // Queue subscription for load balancing
            // For NATS.Net client, the queue group is passed as the second parameter
            asyncEnumerable = _connection.SubscribeAsync<T>(subject, queueGroup, cancellationToken: cancellationToken);
        }

        await foreach (var msg in asyncEnumerable)
        {
            if (msg.Data != null)
            {
                _logger.LogDebug("Received message from subject: {Subject}", subject);

                // Set the reply-to subject if available
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    msg.Data.ReplyTo = msg.ReplyTo;
                }

                yield return msg.Data;
            }
            else
            {
                _logger.LogWarning("Received null message data from subject: {Subject}", subject);
            }
        }
    }

    /// <summary>
    /// Disposes the NATS connection
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
            _isConnected = false;
            _logger.LogInformation("NATS connection disposed");
        }

        // Call GC.SuppressFinalize to prevent derived types from needing to re-implement IDisposable
        GC.SuppressFinalize(this);
    }
}
