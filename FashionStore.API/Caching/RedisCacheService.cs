using StackExchange.Redis;

namespace FashionStore.API.Caching;

public interface IRedisCacheService
{
    Task<byte[]?> GetAsync(string key);
    Task SetAsync(string key, byte[] value, string tag, TimeSpan expiration);
    Task InvalidateTagAsync(string tag);
}

internal sealed class RedisCacheService(IConnectionMultiplexer connection, RedisCacheOptions options) : IRedisCacheService
{
    private readonly IDatabase _database = connection.GetDatabase();

    public async Task<byte[]?> GetAsync(string key)
    {
        var value = await _database.StringGetAsync(Key(key));
        return value.HasValue ? (byte[]?)value : null;
    }

    public async Task SetAsync(string key, byte[] value, string tag, TimeSpan expiration)
    {
        var cacheKey = Key(key);
        var tagKey = TagKey(tag);
        var transaction = _database.CreateTransaction();
        _ = transaction.StringSetAsync(cacheKey, value, expiration);
        _ = transaction.SetAddAsync(tagKey, cacheKey);
        _ = transaction.KeyExpireAsync(tagKey, expiration);
        await transaction.ExecuteAsync();
    }

    public async Task InvalidateTagAsync(string tag)
    {
        var tagKey = TagKey(tag);
        var keys = await _database.SetMembersAsync(tagKey);
        if (keys.Length > 0)
        {
            await _database.KeyDeleteAsync(keys.Select(value => (RedisKey)value.ToString()).ToArray());
        }

        await _database.KeyDeleteAsync(tagKey);
    }

    private string Key(string key) => $"{options.KeyPrefix}:response:{key}";
    private string TagKey(string tag) => $"{options.KeyPrefix}:tag:{tag}";
}
