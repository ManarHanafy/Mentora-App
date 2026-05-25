using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace api.Infrastructure.Caching;

/// <summary>
/// Cache service backed by <see cref="IDistributedCache"/>.
/// Uses <c>AddDistributedMemoryCache()</c> in development and can be swapped to
/// Redis (or any other IDistributedCache provider) in production without code changes.
/// </summary>
public class AppCacheService(IDistributedCache cache) : IAppCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        if (bytes is not null)
        {
            var cached = JsonSerializer.Deserialize<T>(bytes, JsonOptions);
            if (cached is not null)
                return cached;
        }

        var value = await factory();

        if (value is not null)
        {
            var serialized = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await cache.SetAsync(key, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, cancellationToken);
        }

        return value!;
    }

    public void Remove(string key) => cache.Remove(key);

    public void RemoveMany(params string[] keys)
    {
        foreach (var key in keys)
            cache.Remove(key);
    }
}
