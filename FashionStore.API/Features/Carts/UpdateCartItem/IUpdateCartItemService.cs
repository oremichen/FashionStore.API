using FashionStore.API.Features.Carts.Shared;

namespace FashionStore.API.Features.Carts.UpdateCartItem;

public interface IUpdateCartItemService
{
    Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, string cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken);
}
