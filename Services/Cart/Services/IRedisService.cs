using StackExchange.Redis;

namespace Cart.Services
{
    public interface IRedisService
    {
        TimeSpan CartTtl { get; }
        bool IsConnected { get; }

        /// <summary>
        /// The connected database, for commands the key/value helpers below don't cover
        /// </summary>
        /// <exception cref="InvalidOperationException">Not connected yet</exception>
        IDatabase Database { get; }

        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}