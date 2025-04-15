using System.Text;
using System.Text.Json;
using Common.Models;
using Common.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Integration.Tests.Fixtures;

/// <summary>
/// A fake implementation of INatsService for testing
/// </summary>
public class FakeNatsService : INatsService
{
    private readonly Dictionary<string, Func<string, Task<string>>> _handlers = new();
    private readonly Dictionary<string, List<Action<string>>> _subscribers = new();
    private bool _isConnected = true;

    /// <summary>
    /// Gets a value indicating whether the service is connected
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Connects to NATS with retry
    /// </summary>
    public Task ConnectWithRetryAsync(int maxRetries = 5, CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes a message to NATS
    /// </summary>
    public Task PublishAsync<T>(string subject, T message, CancellationToken cancellationToken = default)
    {
        var messageJson = JsonSerializer.Serialize(message);

        if (_subscribers.TryGetValue(subject, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                subscriber(messageJson);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes to a subject
    /// </summary>
    public Task SubscribeAsync(string subject, Action<string> handler, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(subject, out var subscribers))
        {
            subscribers = new List<Action<string>>();
            _subscribers[subject] = subscribers;
        }

        subscribers.Add(handler);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes to a subject with a queue group
    /// </summary>
    public Task SubscribeWithQueueAsync(string subject, string queueGroup, Action<string> handler, CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(subject, handler, cancellationToken);
    }

    /// <summary>
    /// Logs information about a queue group subscription (for testing)
    /// </summary>
    public void LogQueueGroupInfo(string subject, string queueGroup)
    {
        // No-op in fake implementation
    }

    /// <summary>
    /// Subscribes to the specified subject
    /// </summary>
    public IAsyncEnumerable<T> SubscribeAsync<T>(string subject, string? queueGroup = null, CancellationToken cancellationToken = default) where T : BaseMessage
    {
        // This is a simplified implementation that doesn't actually yield any values
        // In a real test, you would need to set up specific test data
        return AsyncEnumerable.Empty<T>();
    }

    /// <summary>
    /// Sends a request and waits for a reply
    /// </summary>
    public Task<TResponse?> RequestAsync<TRequest, TResponse>(string subject, TRequest message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        var messageJson = JsonSerializer.Serialize(message);
        return RequestInternalAsync<TResponse>(subject, messageJson, cancellationToken);
    }

    private async Task<TResponse?> RequestInternalAsync<TResponse>(string subject, string message, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        if (_handlers.TryGetValue(subject, out var handler))
        {
            var responseJson = await handler(message);
            return JsonSerializer.Deserialize<TResponse>(responseJson);
        }

        // Default responses for common requests
        if (subject == "products.get")
        {
            return CreateProductResponse(message) as TResponse;
        }
        else if (subject == "products.create")
        {
            return CreateProductResponse(message) as TResponse;
        }
        else if (subject == "products.update.inventory")
        {
            return CreateProductResponse(message) as TResponse;
        }
        else if (subject == "products.list")
        {
            return CreateProductListResponse() as TResponse;
        }
        else if (subject == "cart.get" || subject == "cart.add" || subject == "cart.update")
        {
            return CreateCartResponse() as TResponse;
        }
        else if (subject == "cart.remove" || subject == "cart.clear")
        {
            return CreateEmptyCartResponse() as TResponse;
        }
        else if (subject == "recommendations.get")
        {
            return CreateRecommendationsResponse() as TResponse;
        }

        // Return a default error response if no handler is found
        return new BaseResponse
        {
            Success = false,
            Error = "No handler found for subject: " + subject
        } as TResponse;
    }

    /// <summary>
    /// Registers a handler for a subject
    /// </summary>
    public void RegisterHandler(string subject, Func<string, Task<string>> handler)
    {
        _handlers[subject] = handler;
    }

    private ProductMessage CreateProductResponse(string productId)
    {
        return new ProductMessage
        {
            ProductId = productId,
            Name = "Test Product",
            Description = "Test Description",
            Price = 19.99m,
            Sku = "TEST-SKU",
            Location = "Test Location",
            QuantityInStock = 100
        };
    }

    private object CreateProductListResponse()
    {
        var products = new List<ProductMessage>();

        for (int i = 0; i < 10; i++)
        {
            products.Add(new ProductMessage
            {
                ProductId = $"test-product-{i}",
                Name = $"Test Product {i}",
                Description = $"Test Description {i}",
                Price = 19.99m + i,
                Sku = $"TEST-SKU-{i}",
                Location = "Test Location",
                QuantityInStock = 100 - i
            });
        }

        return new BaseResponse
        {
            Success = true
        };
    }

    private object CreateCartResponse()
    {
        return new BaseResponse
        {
            Success = true
        };
    }

    private object CreateEmptyCartResponse()
    {
        return new BaseResponse
        {
            Success = true
        };
    }

    private object CreateRecommendationsResponse()
    {
        return new BaseResponse
        {
            Success = true
        };
    }

    /// <summary>
    /// Disposes the service
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _handlers.Clear();
        _subscribers.Clear();
        return ValueTask.CompletedTask;
    }
}
