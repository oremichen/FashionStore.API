using FashionStore.API.Features.Carts.Shared;

namespace FashionStore.API.Features.Carts.ClearCart;

public interface IClearCartService
{
    Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, CancellationToken cancellationToken);
}
