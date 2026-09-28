using FashionStore.API.Features.Carts.Shared;

namespace FashionStore.API.Features.Carts.GetCart;

public interface IGetCartService
{
    Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, CancellationToken cancellationToken);
}
