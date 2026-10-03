namespace FashionStore.API.Features.Wishlists.RemoveWishlistItem;

public interface IRemoveWishlistItemService
{
    Task<ResponseResult> ExecuteAsync(string userId, string wishlistItemId, CancellationToken cancellationToken);
}
