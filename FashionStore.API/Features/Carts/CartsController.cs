using FashionStore.API.Features.Carts.AddCartItem;
using FashionStore.API.Features.Carts.ClearCart;
using FashionStore.API.Features.Carts.DeleteCartItem;
using FashionStore.API.Features.Carts.GetCart;
using FashionStore.API.Features.Carts.Shared;
using FashionStore.API.Features.Carts.UpdateCartItem;
using FashionStore.API.RateLimiting;

namespace FashionStore.API.Features.Carts;

[Authorize]
[ApiController]
[Route("api/carts")]
[EnableRateLimiting(RateLimitPolicies.Cart)]
public sealed class CartsController(
    IGetCartService getCartService,
    IAddCartItemService addCartItemService,
    IUpdateCartItemService updateCartItemService,
    IDeleteCartItemService deleteCartItemService,
    IClearCartService clearCartService) : BaseApiController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(ResponseResult<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await getCartService.ExecuteAsync(userId, cancellationToken));
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ResponseResult<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await addCartItemService.ExecuteAsync(userId, request, cancellationToken));
    }

    [HttpPatch("items/{cartItemId}")]
    [ProducesResponseType(typeof(ResponseResult<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(string cartItemId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await updateCartItemService.ExecuteAsync(userId, cartItemId, request, cancellationToken));
    }

    [HttpDelete("items/{cartItemId}")]
    [ProducesResponseType(typeof(ResponseResult<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteItem(string cartItemId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await deleteCartItemService.ExecuteAsync(userId, cartItemId, cancellationToken));
    }

    [HttpDelete("items")]
    [ProducesResponseType(typeof(ResponseResult<CartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        }

        return ProcessResponse(await clearCartService.ExecuteAsync(userId, cancellationToken));
    }
}
