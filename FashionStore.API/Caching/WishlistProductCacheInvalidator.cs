namespace FashionStore.API.Caching;

public interface IWishlistProductCacheInvalidator
{
    Task InvalidateAsync();
}

internal sealed class WishlistProductCacheInvalidator(
    IRedisCacheService cache,
    ILogger<WishlistProductCacheInvalidator> logger) : IWishlistProductCacheInvalidator
{
    private const string ProductsCacheTag = "products";

    public async Task InvalidateAsync()
    {
        try
        {
            await cache.InvalidateTagAsync(ProductsCacheTag);
            logger.LogInformation("Invalidated product response cache after a wishlist mutation.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to invalidate the product response cache after a wishlist mutation.");
        }
    }
}
