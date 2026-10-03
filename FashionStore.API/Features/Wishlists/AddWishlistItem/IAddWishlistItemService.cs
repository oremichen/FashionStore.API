using FashionStore.API.Features.Wishlists.Shared;

namespace FashionStore.API.Features.Wishlists.AddWishlistItem;

public interface IAddWishlistItemService
{
    Task<ResponseResult<WishlistItemMutationResponse>> ExecuteAsync(string userId, AddWishlistItemRequest request, CancellationToken cancellationToken);
}
