using FashionStore.API.Features.Wishlists.Shared;

namespace FashionStore.API.Features.Wishlists.GetWishlist;

public interface IGetWishlistService
{
    Task<ResponseResult<WishlistResponse>> ExecuteAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);
}
