namespace api.Infrastructure.Caching;

public interface IAppCacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default);
    void Remove(string key);
    void RemoveMany(params string[] keys);
}
