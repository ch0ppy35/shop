using Gateway.Middleware;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.Hubs;

/// <summary>
/// Pushes catalog and cart changes to browsers. Push only: commands still go through the REST API.
/// </summary>
/// <remarks>
/// Server → client methods: <c>ProductChanged</c> (<see cref="Common.Models.ProductChangedEvent"/>, sent to everyone)
/// and <c>CartChanged</c> (<see cref="Common.Models.CartChangedEvent"/>, sent to the session's connections only,
/// which keeps several tabs of one shopper in sync).
/// </remarks>
public class ShopHub : Hub
{
    /// <summary>
    /// The route the hub is mapped to
    /// </summary>
    public const string Route = "/hubs/shop";

    /// <summary>
    /// Gets the group holding every connection of one session
    /// </summary>
    public static string SessionGroup(string sessionId) => $"session:{sessionId}";

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        // SessionMiddleware has already validated and normalized the ID from the query string
        if (Context.GetHttpContext()?.Items["SessionId"] is string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
        }

        await base.OnConnectedAsync();
    }
}
