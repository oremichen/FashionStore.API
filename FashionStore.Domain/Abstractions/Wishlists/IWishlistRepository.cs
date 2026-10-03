using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Wishlists;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserIdAsync(string userId, bool trackChanges, CancellationToken cancellationToken);
    Task<(IReadOnlyList<WishlistItem> Items, int TotalCount)> GetItemsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> GetProductIdsAsync(string? userId, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken);
    Task<Wishlist> AddItemAsync(string userId, string productId, CancellationToken cancellationToken);
    Task<Wishlist?> RemoveItemAsync(string userId, string wishlistItemId, CancellationToken cancellationToken);
    Task<Wishlist?> ClearAsync(string userId, CancellationToken cancellationToken);
}
