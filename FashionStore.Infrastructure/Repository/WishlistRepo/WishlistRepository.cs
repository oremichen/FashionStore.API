using FashionStore.Domain.Abstractions.Wishlists;
using FashionStore.Domain.Entities;
using FashionStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.WishlistRepo;

public sealed class WishlistRepository(FashionStoreDbContext dbContext) : IWishlistRepository
{
    public Task<Wishlist?> GetByUserIdAsync(string userId, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Wishlist> query = dbContext.Wishlists;
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(wishlist => wishlist.UserId == userId, cancellationToken);
    }

    public async Task<(IReadOnlyList<WishlistItem> Items, int TotalCount)> GetItemsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.WishlistItems.AsNoTracking()
            .Where(item => item.Wishlist.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(item => item.Product)
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<IReadOnlySet<string>> GetProductIdsAsync(string? userId, IReadOnlyCollection<string> productIds, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || productIds.Count == 0)
        {
            return new HashSet<string>();
        }

        return await dbContext.WishlistItems.AsNoTracking()
            .Where(item => item.Wishlist.UserId == userId && productIds.Contains(item.ProductId))
            .Select(item => item.ProductId)
            .ToHashSetAsync(cancellationToken);
    }

    public async Task<Wishlist> AddItemAsync(string userId, string productId, CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(async cancellationTokenInner =>
        {
            await EnsureWishlistExistsAsync(userId, cancellationTokenInner);
            var wishlist = await GetLockedByUserIdAsync(userId, cancellationTokenInner)
                ?? throw new InvalidOperationException("The wishlist could not be created.");
            wishlist.AddItem(productId);
            await dbContext.SaveChangesAsync(cancellationTokenInner);
            return wishlist;
        }, cancellationToken);
    }

    public async Task<Wishlist?> RemoveItemAsync(string userId, string wishlistItemId, CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(async cancellationTokenInner =>
        {
            var wishlist = await GetLockedByUserIdAsync(userId, cancellationTokenInner);
            if (wishlist is null)
            {
                return null;
            }

            wishlist.RemoveItem(wishlistItemId);
            await dbContext.SaveChangesAsync(cancellationTokenInner);
            return wishlist;
        }, cancellationToken);
    }

    public async Task<Wishlist?> ClearAsync(string userId, CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(async cancellationTokenInner =>
        {
            var wishlist = await GetLockedByUserIdAsync(userId, cancellationTokenInner);
            if (wishlist is null)
            {
                return null;
            }

            wishlist.ClearItems();
            await dbContext.SaveChangesAsync(cancellationTokenInner);
            return wishlist;
        }, cancellationToken);
    }

    private Task EnsureWishlistExistsAsync(string userId, CancellationToken cancellationToken)
    {
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Wishlists\" (\"UserId\") VALUES ({userId}) ON CONFLICT (\"UserId\") DO NOTHING;",
            cancellationToken);
    }

    private Task<Wishlist?> GetLockedByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return dbContext.Wishlists
            .FromSqlInterpolated($"SELECT * FROM \"Wishlists\" WHERE \"UserId\" = {userId} FOR UPDATE")
            .Include(wishlist => wishlist.Items)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        return dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async cancellationTokenInner =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationTokenInner);
            try
            {
                var result = await operation(cancellationTokenInner);
                await transaction.CommitAsync(cancellationTokenInner);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationTokenInner);
                dbContext.ChangeTracker.Clear();
                throw;
            }
        }, cancellationToken);
    }
}
