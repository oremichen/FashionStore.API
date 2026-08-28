using System.Security.Claims;
using System.Text;

namespace FashionStore.API.Caching;

public sealed class RedisResponseCacheMiddleware(
    RequestDelegate next,
    IRedisCacheService cache,
    RedisCacheOptions options,
    ILogger<RedisResponseCacheMiddleware> logger)
{
    private static readonly IReadOnlyDictionary<string, string> CacheableRoutes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/api/products"] = "products",
            ["/api/contact-us"] = "contacts",
            ["/api/main-carousels"] = "main-carousels",
            ["/api/users"] = "users",
            ["/api/categories"] = "catalog",
            ["/api/brands"] = "catalog",
            ["/api/colors"] = "catalog",
            ["/api/sizes"] = "catalog",
            ["/api/promotion-banners"] = "promotions",
            ["/api/promotion-videos"] = "promotions"
        };

    public async Task InvokeAsync(HttpContext context)
    {
        var tag = FindTag(context.Request.Path);
        if (tag is null)
        {
            await next(context);
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            await HandleReadAsync(context, tag);
            return;
        }

        await next(context);
        if (IsMutation(context.Request.Method) && context.Response.StatusCode is >= 200 and < 300)
        {
            await TryInvalidateAsync(tag);

            // Product responses embed catalog data, so catalog writes also invalidate products.
            if (tag == "catalog")
            {
                await TryInvalidateAsync("products");
            }
        }
    }

    private async Task HandleReadAsync(HttpContext context, string tag)
    {
        var key = BuildKey(context);
        try
        {
            var cached = await cache.GetAsync(key);
            if (cached is not null)
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.Headers["X-Cache"] = "HIT";
                await context.Response.Body.WriteAsync(cached, context.RequestAborted);
                return;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis cache read failed for {Path}; continuing without cache.", context.Request.Path);
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await next(context);
            buffer.Position = 0;

            if (context.Response.StatusCode == StatusCodes.Status200OK &&
                context.Response.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                var payload = buffer.ToArray();
                try
                {
                    await cache.SetAsync(key, payload, tag, TimeSpan.FromHours(options.AbsoluteExpirationHours));
                    context.Response.Headers["X-Cache"] = "MISS";
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Redis cache write failed for {Path}; continuing without cache.", context.Request.Path);
                }
            }

            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private async Task TryInvalidateAsync(string tag)
    {
        try
        {
            await cache.InvalidateTagAsync(tag);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis cache invalidation failed for tag {CacheTag}.", tag);
        }
    }

    private static string? FindTag(PathString path) => CacheableRoutes
        .Where(route => path.StartsWithSegments(route.Key))
        .OrderByDescending(route => route.Key.Length)
        .Select(route => route.Value)
        .FirstOrDefault();

    private static bool IsMutation(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    private static string BuildKey(HttpContext context)
    {
        var identity = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var raw = $"{identity}|{context.Request.Path.Value?.ToLowerInvariant()}|{context.Request.QueryString.Value}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
