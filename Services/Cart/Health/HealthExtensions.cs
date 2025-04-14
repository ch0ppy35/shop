using Microsoft.Extensions.DependencyInjection;

namespace Cart.Health;

/// <summary>
/// Extension methods for health checks
/// </summary>
public static class HealthExtensions
{
    /// <summary>
    /// Adds Redis health check to the service collection
    /// </summary>
    public static IServiceCollection AddRedisHealthCheck(this IServiceCollection services)
    {
        services.AddSingleton<RedisHealthCheck>();
        return services;
    }
}
