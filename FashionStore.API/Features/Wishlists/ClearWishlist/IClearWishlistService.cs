namespace FashionStore.API.Features.Wishlists.ClearWishlist;

public interface IClearWishlistService
{
    Task<ResponseResult> ExecuteAsync(string userId, CancellationToken cancellationToken);
}
