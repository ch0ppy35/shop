namespace Common.Health;

/// <summary>
/// Interface for health check providers
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Gets the name of the health check
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Checks if the component is ready
    /// </summary>
    /// <returns>True if the component is ready, false otherwise</returns>
    bool IsReady();
}
