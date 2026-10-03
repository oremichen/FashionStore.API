using FashionStore.Domain.Abstractions.Wishlists;
using FashionStore.API.Caching;

namespace FashionStore.API.Features.Wishlists.ClearWishlist;

public sealed class ClearWishlistService(
    IWishlistRepository wishlistRepository,
    IWishlistProductCacheInvalidator productCacheInvalidator,
    ILogger<ClearWishlistService> logger) : IClearWishlistService
{
    public async Task<ResponseResult> ExecuteAsync(string userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Clearing wishlist for user {UserId}.", userId);
        var wishlist = await wishlistRepository.ClearAsync(userId, cancellationToken);
        if (wishlist is null)
        {
            return new ResponseResult().Fail("The wishlist was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        await productCacheInvalidator.InvalidateAsync();
        logger.LogInformation("Wishlist {WishlistId} was cleared for user {UserId}.", wishlist.Id, userId);
        var response = new ResponseResult().Success("Wishlist cleared successfully.");
        response.StatusCode = ResponseCodes.SUCCESS;
        return response;
    }
}
