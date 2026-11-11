using Common.Messaging;
using Common.Models;
using Gateway.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.Services;

/// <summary>
/// Turns domain events from NATS into cache invalidations and SignalR pushes
/// </summary>
/// <remarks>
/// Subscribes without a queue group: every gateway instance must drop its own in-memory cache entries and
/// notify the browsers connected to it, so every instance needs every event. That also means no SignalR
/// backplane is needed when the gateway is scaled out.
/// </remarks>
public class GatewayEventService : BackgroundService
{
    private readonly INatsService _natsService;
    private readonly CatalogCache _catalogCache;
    private readonly IHubContext<ShopHub> _hub;
    private readonly ILogger<GatewayEventService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatewayEventService"/> class.
    /// </summary>
    public GatewayEventService(INatsService natsService, CatalogCache catalogCache, IHubContext<ShopHub> hub,
        ILogger<GatewayEventService> logger)
    {
        _natsService = natsService;
        _catalogCache = catalogCache;
        _hub = hub;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!_natsService.IsConnected)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
        }

        await Task.WhenAll(
            ForwardAsync<ProductChangedEvent>(Subjects.ProductsChanged, OnProductChangedAsync, stoppingToken),
            ForwardAsync<CartChangedEvent>(Subjects.AllCartsChanged, OnCartChangedAsync, stoppingToken));
    }

    /// <summary>
    /// Invalidates the cache first, so a browser that refetches on the push already gets the new data
    /// </summary>
    public async Task OnProductChangedAsync(ProductChangedEvent change, CancellationToken cancellationToken)
    {
        await _catalogCache.InvalidateAsync(change, cancellationToken);
        await _hub.Clients.All.SendAsync("ProductChanged", change, cancellationToken);
    }

    /// <summary>
    /// Sends the new cart to the session's open tabs
    /// </summary>
    public async Task OnCartChangedAsync(CartChangedEvent change, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(change.SessionId))
        {
            return;
        }

        await _hub.Clients.Group(ShopHub.SessionGroup(change.SessionId))
            .SendAsync("CartChanged", change, cancellationToken);
    }

    private async Task ForwardAsync<T>(string subject, Func<T, CancellationToken, Task> handle,
        CancellationToken stoppingToken) where T : BaseMessage
    {
        _logger.LogInformation("Forwarding {Subject} events", subject);

        try
        {
            await foreach (var message in _natsService.SubscribeAsync<T>(subject, cancellationToken: stoppingToken))
            {
                try
                {
                    await handle(message, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error handling {Subject} event", subject);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stopped forwarding {Subject} events", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Subject} event forwarding failed", subject);
        }
    }
}
