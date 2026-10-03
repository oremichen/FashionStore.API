using FashionStore.API.Features.Wishlists.Shared;
using FashionStore.API.Caching;
using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Abstractions.Wishlists;

namespace FashionStore.API.Features.Wishlists.AddWishlistItem;

public sealed class AddWishlistItemService(
    IWishlistRepository wishlistRepository,
    IProductRepository productRepository,
    IWishlistProductCacheInvalidator productCacheInvalidator,
    ILogger<AddWishlistItemService> logger) : IAddWishlistItemService
{
    public async Task<ResponseResult<WishlistItemMutationResponse>> ExecuteAsync(string userId, AddWishlistItemRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding product {ProductId} to the wishlist for user {UserId}.", request.ProductId, userId);
        var product = await productRepository.GetByIdAsync(request.ProductId, false, cancellationToken);
        if (product is null || !product.IsActive || product.IsArchived)
        {
            logger.LogError("Wishlist addition rejected because product {ProductId} is unavailable for user {UserId}.", request.ProductId, userId);
            return new ResponseResult<WishlistItemMutationResponse>().Fail("The product is unavailable.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        try
        {
            var wishlist = await wishlistRepository.AddItemAsync(userId, product.Id, cancellationToken);
            var item = wishlist.Items.Single(item => item.ProductId == product.Id);
            await productCacheInvalidator.InvalidateAsync();
            logger.LogInformation("Product {ProductId} was added to wishlist {WishlistId} for user {UserId}.", product.Id, wishlist.Id, userId);
            return new ResponseResult<WishlistItemMutationResponse>().Success(new WishlistItemMutationResponse
            {
                WishlistItemId = item.Id,
                ProductId = product.Id,
                IsWishlistItem = true
            }, "Item added to wishlist successfully.");
        }
        catch (ArgumentException exception)
        {
            logger.LogError(exception, "Wishlist item validation failed for user {UserId}, product {ProductId}.", userId, request.ProductId);
            return new ResponseResult<WishlistItemMutationResponse>().Fail("The wishlist item is invalid.", ResponseCodes.INVALID_ACTION);
        }
    }
}
