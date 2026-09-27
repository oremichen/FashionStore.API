using FashionStore.API.Features.Carts.Shared;
using FashionStore.Domain.Abstractions.Carts;

namespace FashionStore.API.Features.Carts.UpdateCartItem;

public sealed class UpdateCartItemService(ICartRepository cartRepository, ILogger<UpdateCartItemService> logger) : IUpdateCartItemService
{
    public async Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, string cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating cart item {CartItemId} for user {UserId}.", cartItemId, userId);
        try
        {
            var cart = await cartRepository.UpdateItemQuantityAsync(userId, cartItemId, request.Quantity, cancellationToken);
            if (cart is null)
            {
                return new ResponseResult<CartResponse>().Fail("The cart was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            return new ResponseResult<CartResponse>().Success(CartResponseMapper.Map(cart), "Cart item updated successfully.");
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Cart item {CartItemId} was not found for user {UserId}.", cartItemId, userId);
            return new ResponseResult<CartResponse>().Fail("The cart item was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }
    }
}
