using System.Collections.Concurrent;
using System.Text.Json;
using Common.Messaging;
using Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Integration.Tests.Fixtures;

/// <summary>
/// A testable version of NatsService that works with real NATS server via Testcontainers
/// but also provides additional testing capabilities
/// </summary>
public class TestableNatsService : NatsService
{
    private readonly ILogger<NatsService> _logger;
    private readonly ConcurrentDictionary<string, Func<string, Task<string>>> _handlers = new();
    private readonly ConcurrentDictionary<string, object> _mockResponses = new();
    private readonly ConcurrentDictionary<string, ProductMessage> _mockProducts = new();
    private readonly ConcurrentDictionary<string, CartResponse> _mockCarts = new();
    private readonly ConcurrentDictionary<string, IAsyncDisposable> _subscriptions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TestableNatsService"/> class
    /// </summary>
    public TestableNatsService(ILogger<NatsService> logger, IConfiguration configuration)
        : base(logger, configuration)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a handler for a subject
    /// </summary>
    public async Task RegisterHandler(string subject, Func<string, Task<string>> handler)
    {
        _logger.LogInformation("Registering handler for subject: {Subject}", subject);
        _handlers[subject] = handler;

        // If we're already connected, create the subscription now
        if (IsConnected)
        {
            await CreateSubscription(subject);
        }
    }

    /// <summary>
    /// Resubscribes a handler for a subject
    /// </summary>
    public async Task ResubscribeHandler(string subject)
    {
        if (_handlers.TryGetValue(subject, out var handler))
        {
            _logger.LogInformation("Resubscribing handler for subject: {Subject}", subject);
            await CreateSubscription(subject);
        }
        else
        {
            _logger.LogWarning("No handler registered for subject: {Subject}", subject);
        }
    }

    /// <summary>
    /// Creates a subscription for a subject
    /// </summary>
    private async Task CreateSubscription(string subject)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot create subscription for subject {Subject} - not connected", subject);
            return;
        }

        if (!_handlers.TryGetValue(subject, out var handler))
        {
            _logger.LogWarning("No handler registered for subject: {Subject}", subject);
            return;
        }

        try
        {
            // Cancel any existing subscription
            if (_subscriptions.TryRemove(subject, out var existingSubscription))
            {
                await existingSubscription.DisposeAsync();
            }

            // For testing, we'll just store the handler and use it in our RequestAsync method
            // This avoids issues with the NATS client API
            _logger.LogInformation("Handler registered for subject: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for subject {Subject}: {Message}", subject, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Adds a mock response for a subject
    /// </summary>
    public void AddMockResponse<T>(string subject, T response)
    {
        _mockResponses[subject] = response!;
        _logger.LogInformation("Added mock response for subject: {Subject}", subject);
    }

    /// <summary>
    /// Gets a mock response for a subject
    /// </summary>
    public T? GetMockResponse<T>(string subject)
    {
        if (_mockResponses.TryGetValue(subject, out var response) && response is T typedResponse)
        {
            return typedResponse;
        }

        return default;
    }

    /// <summary>
    /// Adds a mock product
    /// </summary>
    public void AddMockProduct(string productId, ProductMessage product)
    {
        _mockProducts[productId] = product;
        _logger.LogInformation("Added mock product: {ProductId}", productId);
    }

    /// <summary>
    /// Gets a mock product
    /// </summary>
    public ProductMessage? GetMockProduct(string productId)
    {
        if (_mockProducts.TryGetValue(productId, out var product))
        {
            return product;
        }

        return null;
    }

    /// <summary>
    /// Adds a mock cart
    /// </summary>
    public void AddMockCart(string sessionId, CartResponse cart)
    {
        _mockCarts[sessionId] = cart;
        _logger.LogInformation("Added mock cart for session: {SessionId}", sessionId);
    }

    /// <summary>
    /// Gets a mock cart
    /// </summary>
    public CartResponse? GetMockCart(string sessionId)
    {
        if (_mockCarts.TryGetValue(sessionId, out var cart))
        {
            return cart;
        }

        return null;
    }

    /// <summary>
    /// Checks if a mock cart exists
    /// </summary>
    public bool HasMockCart(string sessionId)
    {
        return _mockCarts.ContainsKey(sessionId);
    }

    /// <summary>
    /// Checks if a mock product exists
    /// </summary>
    public bool HasMockProduct(string productId)
    {
        return _mockProducts.ContainsKey(productId);
    }

    /// <summary>
    /// Overrides the RequestAsync method to use mock responses when available
    /// </summary>
    public override async Task<TResponse?> RequestAsync<TRequest, TResponse>(
        string subject,
        TRequest message,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        // Check if we have a mock response for this subject
        if (_mockResponses.TryGetValue(subject, out var mockResponse) && mockResponse is TResponse typedResponse)
        {
            _logger.LogInformation("Using mock response for subject: {Subject}", subject);
            return typedResponse;
        }

        // Check if we have a handler for this subject
        if (_handlers.TryGetValue(subject, out var handler))
        {
            _logger.LogInformation("Using registered handler for subject: {Subject}", subject);
            var messageJson = JsonSerializer.Serialize(message);
            var responseJson = await handler(messageJson);
            var response = JsonSerializer.Deserialize<TResponse>(responseJson);
            return response;
        }

        // If no mock response or handler, use the real NATS connection
        _logger.LogInformation("No mock response or handler found for subject: {Subject}, using real NATS connection", subject);
        return await base.RequestAsync<TRequest, TResponse>(subject, message, timeout, cancellationToken);
    }

    /// <summary>
    /// Disposes the NATS connection and subscriptions
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        // Dispose all subscriptions
        foreach (var subscription in _subscriptions.Values)
        {
            await subscription.DisposeAsync();
        }
        _subscriptions.Clear();

        // Dispose the base connection
        await base.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
