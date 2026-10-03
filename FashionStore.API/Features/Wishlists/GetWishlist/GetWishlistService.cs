using FashionStore.API.Features.Wishlists.Shared;
using FashionStore.Domain.Abstractions.Wishlists;

namespace FashionStore.API.Features.Wishlists.GetWishlist;

public sealed class GetWishlistService(IWishlistRepository wishlistRepository, ILogger<GetWishlistService> logger) : IGetWishlistService
{
    public async Task<ResponseResult<WishlistResponse>> ExecuteAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return new ResponseResult<WishlistResponse>().Fail("Page and pageSize are invalid.", ResponseCodes.INVALID_ACTION);
        }

        logger.LogInformation("Retrieving wishlist page {Page} with page size {PageSize} for user {UserId}.", page, pageSize, userId);
        var (items, totalCount) = await wishlistRepository.GetItemsAsync(userId, page, pageSize, cancellationToken);
        return new ResponseResult<WishlistResponse>().Success(new WishlistResponse
        {
            Items = items.Select(WishlistResponseMapper.Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        }, "Wishlist retrieved successfully.");
    }
}
