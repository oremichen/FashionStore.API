using FashionStore.API.Features.Carts.Shared;
using FashionStore.Domain.Abstractions.Carts;

namespace FashionStore.API.Features.Carts.DeleteCartItem;

public sealed class DeleteCartItemService(ICartRepository cartRepository, ILogger<DeleteCartItemService> logger) : IDeleteCartItemService
{
    public async Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, string cartItemId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing cart item {CartItemId} for user {UserId}.", cartItemId, userId);
        try
        {
            var cart = await cartRepository.RemoveItemAsync(userId, cartItemId, cancellationToken);
            if (cart is null)
            {
                return new ResponseResult<CartResponse>().Fail("The cart was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            return new ResponseResult<CartResponse>().Success(CartResponseMapper.Map(cart), "Cart item removed successfully.");
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Cart item {CartItemId} was not found for user {UserId}.", cartItemId, userId);
            return new ResponseResult<CartResponse>().Fail("The cart item was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }
    }
}
