namespace FashionStore.API.Caching;

public sealed class RedisCacheOptions
{
    public const string SectionName = "RedisCache";

    public bool BypassRedis { get; init; }
    public int AbsoluteExpirationHours { get; init; } = 24;
    public string KeyPrefix { get; init; } = "fashion-store";
}
