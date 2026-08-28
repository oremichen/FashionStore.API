using StackExchange.Redis;

namespace FashionStore.API.Caching;

public static class RedisCachingServiceCollectionExtensions
{
    public static IServiceCollection AddRedisResponseCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException(
                "Redis is not configured. Set the ConnectionStrings__Redis environment variable.");

        var options = configuration.GetSection(RedisCacheOptions.SectionName).Get<RedisCacheOptions>()
            ?? new RedisCacheOptions();

        if (options.AbsoluteExpirationHours <= 0)
        {
            throw new InvalidOperationException("RedisCache:AbsoluteExpirationHours must be greater than zero.");
        }

        services.AddSingleton(options);
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(ParseConnectionString(connectionString)));
        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        return services;
    }

    private static ConfigurationOptions ParseConnectionString(string connectionString)
    {
        var normalized = connectionString.Replace("rediss\\://", "rediss://", StringComparison.OrdinalIgnoreCase);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase))
        {
            return ConfigurationOptions.Parse(normalized);
        }

        return new ConfigurationOptions
        {
            EndPoints = { { uri.Host, uri.Port > 0 ? uri.Port : 6380 } },
            User = string.IsNullOrWhiteSpace(uri.UserInfo) ? null : Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[0]),
            Password = uri.UserInfo.Contains(':') ? Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[1]) : null,
            Ssl = true,
            AbortOnConnectFail = false
        };
    }
}
