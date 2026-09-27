using FashionStore.API.Features.Carts.Shared;

namespace FashionStore.API.Features.Carts.AddCartItem;

public interface IAddCartItemService
{
    Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, AddCartItemRequest request, CancellationToken cancellationToken);
}
