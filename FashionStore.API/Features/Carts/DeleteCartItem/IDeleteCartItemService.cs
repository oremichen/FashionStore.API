using FashionStore.API.Features.Carts.Shared;

namespace FashionStore.API.Features.Carts.DeleteCartItem;

public interface IDeleteCartItemService
{
    Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, string cartItemId, CancellationToken cancellationToken);
}
