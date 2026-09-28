using FashionStore.API.Features.Carts.Shared;
using FashionStore.Domain.Abstractions.Carts;

namespace FashionStore.API.Features.Carts.GetCart;

public sealed class GetCartService(ICartRepository cartRepository, ILogger<GetCartService> logger) : IGetCartService
{
    public async Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving cart for user {UserId}.", userId);
        var cart = await cartRepository.GetByUserIdAsync(userId, false, cancellationToken);
        if (cart is null)
        {
            return new ResponseResult<CartResponse>().Fail("The cart was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        return new ResponseResult<CartResponse>().Success(CartResponseMapper.Map(cart), "Cart retrieved successfully.");
    }
}
