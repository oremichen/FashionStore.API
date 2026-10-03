using FashionStore.Domain.Abstractions.Wishlists;
using FashionStore.API.Caching;

namespace FashionStore.API.Features.Wishlists.RemoveWishlistItem;

public sealed class RemoveWishlistItemService(
    IWishlistRepository wishlistRepository,
    IWishlistProductCacheInvalidator productCacheInvalidator,
    ILogger<RemoveWishlistItemService> logger) : IRemoveWishlistItemService
{
    public async Task<ResponseResult> ExecuteAsync(string userId, string wishlistItemId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing wishlist item {WishlistItemId} for user {UserId}.", wishlistItemId, userId);
        try
        {
            var wishlist = await wishlistRepository.RemoveItemAsync(userId, wishlistItemId, cancellationToken);
            if (wishlist is null)
            {
                return new ResponseResult().Fail("The wishlist was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            await productCacheInvalidator.InvalidateAsync();
            var response = new ResponseResult().Success("Wishlist item removed successfully.");
            response.StatusCode = ResponseCodes.SUCCESS;
            return response;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError(exception, "Wishlist item {WishlistItemId} was not found for user {UserId}.", wishlistItemId, userId);
            return new ResponseResult().Fail("The wishlist item was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }
    }
}
