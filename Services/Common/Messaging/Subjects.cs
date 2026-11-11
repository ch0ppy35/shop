namespace Common.Messaging;

/// <summary>
/// NATS subjects for domain events that services publish after a write
/// </summary>
public static class Subjects
{
    /// <summary>
    /// A product was created, updated, re-stocked or deleted
    /// </summary>
    public const string ProductsChanged = "products.changed";

    /// <summary>
    /// Wildcard that matches every session's cart change
    /// </summary>
    public const string AllCartsChanged = "cart.changed.*";

    /// <summary>
    /// The subject a session's cart changes are published on
    /// </summary>
    /// <param name="sessionId">A session ID; the gateway only accepts GUIDs, so it is a single subject token</param>
    public static string CartChanged(string sessionId) => $"cart.changed.{sessionId}";
}
