using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Carts;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(string userId, bool trackChanges, CancellationToken cancellationToken);
    Task<Cart?> GetForCheckoutAsync(string cartId, string userId, CancellationToken cancellationToken);
    Task<Cart> AddOrIncreaseItemAsync(string userId, string productId, string? variantId, string? colorId, int quantity, CancellationToken cancellationToken);
    Task<Cart?> UpdateItemQuantityAsync(string userId, string cartItemId, int quantity, CancellationToken cancellationToken);
    Task<Cart?> RemoveItemAsync(string userId, string cartItemId, CancellationToken cancellationToken);
    Task<Cart?> ClearAsync(string userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
