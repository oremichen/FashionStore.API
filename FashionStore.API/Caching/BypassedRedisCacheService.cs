namespace FashionStore.API.Caching;

internal sealed class BypassedRedisCacheService : IRedisCacheService
{
    public Task<byte[]?> GetAsync(string key)
    {
        return Task.FromResult<byte[]?>(null);
    }

    public Task SetAsync(string key, byte[] value, string tag, TimeSpan expiration)
    {
        return Task.CompletedTask;
    }

    public Task InvalidateTagAsync(string tag)
    {
        return Task.CompletedTask;
    }

    public Task<long> PurgeResponseCacheAsync()
    {
        return Task.FromResult(0L);
    }
}
