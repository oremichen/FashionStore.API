using FashionStore.API.Features.Wishlists.AddWishlistItem;
using FashionStore.API.Features.Wishlists.ClearWishlist;
using FashionStore.API.Features.Wishlists.GetWishlist;
using FashionStore.API.Features.Wishlists.RemoveWishlistItem;
using FashionStore.API.Features.Wishlists.Shared;
using FashionStore.API.RateLimiting;

namespace FashionStore.API.Features.Wishlists;

[Authorize]
[ApiController]
[Route("api/wishlists")]
[EnableRateLimiting(RateLimitPolicies.Wishlist)]
public sealed class WishlistsController(
    IGetWishlistService getWishlistService,
    IAddWishlistItemService addWishlistItemService,
    IRemoveWishlistItemService removeWishlistItemService,
    IClearWishlistService clearWishlistService) : BaseApiController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(ResponseResult<WishlistResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await getWishlistService.ExecuteAsync(userId, page, pageSize, cancellationToken));
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ResponseResult<WishlistItemMutationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItem([FromBody] AddWishlistItemRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await addWishlistItemService.ExecuteAsync(userId, request, cancellationToken));
    }

    [HttpDelete("items/{wishlistItemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(string wishlistItemId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await removeWishlistItemService.ExecuteAsync(userId, wishlistItemId, cancellationToken));
    }

    [HttpDelete("items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await clearWishlistService.ExecuteAsync(userId, cancellationToken));
    }
}
