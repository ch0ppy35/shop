using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cart.Services
{
    public interface IRedisService
    {
        TimeSpan CartTtl { get; }
        bool IsConnected { get; }
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
