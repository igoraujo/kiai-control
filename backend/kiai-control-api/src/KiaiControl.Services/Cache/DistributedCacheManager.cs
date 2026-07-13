using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace KiaiControl.Services.Cache;

internal sealed class DistributedCacheManager(IDistributedCache cache)
{
    private const string NamespaceVersionKey = "cache:namespace:version";
    private static readonly DistributedCacheEntryOptions NamespaceVersionOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
    };
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var effectiveKey = await BuildEffectiveKeyAsync(key, cancellationToken);
        var cachedData = await cache.GetStringAsync(effectiveKey, cancellationToken);

        if (cachedData is null)
        {
            return (false, default);
        }

        return (true, JsonSerializer.Deserialize<T>(cachedData, SerializerOptions));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expirationDuration, CancellationToken cancellationToken = default)
    {
        var effectiveKey = await BuildEffectiveKeyAsync(key, cancellationToken);
        var jsonData = JsonSerializer.Serialize(value, SerializerOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expirationDuration
        };

        await cache.SetStringAsync(effectiveKey, jsonData, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var effectiveKey = await BuildEffectiveKeyAsync(key, cancellationToken);
        await cache.RemoveAsync(effectiveKey, cancellationToken);
    }

    public Task RemoveAllAsync(CancellationToken cancellationToken = default)
    {
        var newNamespaceVersion = Guid.NewGuid().ToString("N");
        return cache.SetStringAsync(NamespaceVersionKey, newNamespaceVersion, NamespaceVersionOptions, cancellationToken);
    }

    private async Task<string> BuildEffectiveKeyAsync(string key, CancellationToken cancellationToken)
    {
        var namespaceVersion = await GetNamespaceVersionAsync(cancellationToken);
        return $"{namespaceVersion}:{key}";
    }

    private async Task<string> GetNamespaceVersionAsync(CancellationToken cancellationToken)
    {
        var namespaceVersion = await cache.GetStringAsync(NamespaceVersionKey, cancellationToken);

        if (!string.IsNullOrWhiteSpace(namespaceVersion))
        {
            return namespaceVersion;
        }

        namespaceVersion = "v1";
        await cache.SetStringAsync(NamespaceVersionKey, namespaceVersion, NamespaceVersionOptions, cancellationToken);

        return namespaceVersion;
    }
}