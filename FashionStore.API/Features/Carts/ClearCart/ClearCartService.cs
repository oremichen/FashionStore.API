using FashionStore.API.Features.Carts.Shared;
using FashionStore.Domain.Abstractions.Carts;

namespace FashionStore.API.Features.Carts.ClearCart;

public sealed class ClearCartService(ICartRepository cartRepository, ILogger<ClearCartService> logger) : IClearCartService
{
    public async Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Clearing cart for user {UserId}.", userId);
        var cart = await cartRepository.ClearAsync(userId, cancellationToken);
        if (cart is null)
        {
            return new ResponseResult<CartResponse>().Fail("The cart was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        return new ResponseResult<CartResponse>().Success(CartResponseMapper.Map(cart), "Cart cleared successfully.");
    }
}
