using System.Collections.Concurrent;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.Services.Cache;

internal sealed class DistributedCacheAspect(DistributedCacheManager cacheManager) : ICacheAspect
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<T> ExecuteWithCache<T>(
        Func<Task<T>> method,
        string key,
        TimeSpan expirationDuration,
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await cacheManager.TryGetAsync<T>(key, cancellationToken);
        if (cachedValue.Found)
        {
            return cachedValue.Value!;
        }

        var cacheLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync(cancellationToken);

        try
        {
            cachedValue = await cacheManager.TryGetAsync<T>(key, cancellationToken);
            if (cachedValue.Found)
            {
                return cachedValue.Value!;
            }

            var result = await method();
            if (ShouldCache(result))
            {
                await cacheManager.SetAsync(key, result, expirationDuration, cancellationToken);
            }

            return result;
        }
        finally
        {
            cacheLock.Release();
        }
    }

    public async Task<T> ExecuteWithCache<T>(
        Func<Task<T>> method,
        string key,
        Func<T, TimeSpan> expirationDurationFactory,
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await cacheManager.TryGetAsync<T>(key, cancellationToken);
        if (cachedValue.Found)
        {
            return cachedValue.Value!;
        }

        var cacheLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync(cancellationToken);

        try
        {
            cachedValue = await cacheManager.TryGetAsync<T>(key, cancellationToken);
            if (cachedValue.Found)
            {
                return cachedValue.Value!;
            }

            var result = await method();
            if (ShouldCache(result))
            {
                var expirationDuration = expirationDurationFactory(result);
                await cacheManager.SetAsync(key, result, expirationDuration, cancellationToken);
            }

            return result;
        }
        finally
        {
            cacheLock.Release();
        }
    }

    public Task RemoveCacheAsync(string key, CancellationToken cancellationToken = default)
    {
        return cacheManager.RemoveAsync(key, cancellationToken);
    }

    public Task RemoveCacheAllAsync(CancellationToken cancellationToken = default)
    {
        return cacheManager.RemoveAllAsync(cancellationToken);
    }

    private static bool ShouldCache<T>(T result)
    {
        return result is not null || typeof(T).IsValueType;
    }
}