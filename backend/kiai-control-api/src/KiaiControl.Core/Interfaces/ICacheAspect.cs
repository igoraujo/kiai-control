namespace KiaiControl.Core.Interfaces;

public interface ICacheAspect
{
    Task<T> ExecuteWithCache<T>(
        Func<Task<T>> method,
        string key,
        TimeSpan expirationDuration,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteWithCache<T>(
        Func<Task<T>> method,
        string key,
        Func<T, TimeSpan> expirationDurationFactory,
        CancellationToken cancellationToken = default);

    Task RemoveCacheAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveCacheAllAsync(CancellationToken cancellationToken = default);
}