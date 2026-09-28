using FashionStore.Domain.Abstractions.Carts;
using FashionStore.Domain.Entities;
using FashionStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.CartRepo;

public sealed class CartRepository(FashionStoreDbContext dbContext) : ICartRepository
{
    public Task<Cart?> GetByUserIdAsync(string userId, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Cart> query = dbContext.Carts.Include(cart => cart.Items);
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);
    }

    public Task<Cart?> GetForCheckoutAsync(string cartId, string userId, CancellationToken cancellationToken)
    {
        return GetLockedCartAsync(cartId, userId, cancellationToken);
    }

    public async Task<Cart> AddOrIncreaseItemAsync(
        string userId,
        string productId,
        string? variantId,
        string? colorId,
        int quantity,
        CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(async cancellationTokenInner =>
        {
            await EnsureCartExistsAsync(userId, cancellationTokenInner);
            var cart = await GetLockedCartByUserIdAsync(userId, cancellationTokenInner)
                ?? throw new InvalidOperationException("The cart could not be created.");

            cart.AddItem(productId, variantId, colorId, quantity);
            await dbContext.SaveChangesAsync(cancellationTokenInner);
            return cart;
        }, cancellationToken);
    }

    public async Task<Cart?> UpdateItemQuantityAsync(string userId, string cartItemId, int quantity, CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(async cancellationTokenInner =>
        {
            var cart = await GetLockedCartByUserIdAsync(userId, cancellationTokenInner);
            if (cart is null)
            {
                return null;
            }

            cart.UpdateItemQuantity(cartItemId, quantity);
            await dbContext.SaveChangesAsync(cancellationTokenInner);
            return cart;
        }, cancellationToken);
    }

    public async Task<Cart?> RemoveItemAsync(string userId, string cartItemId, CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(async cancellationTokenInner =>
        {
            var cart = await GetLockedCartByUserIdAsync(userId, cancellationTokenInner);
            if (cart is null)
            {
                return null;
            }

            cart.RemoveItem(cartItemId);
            await dbContext.SaveChangesAsync(cancellationTokenInner);
            return cart;
        }, cancellationToken);
    }

    public async Task<Cart?> ClearAsync(string userId, CancellationToken cancellationToken)
    {
        return await ExecuteInTransactionAsync(async cancellationTokenInner =>
        {
            var cart = await GetLockedCartByUserIdAsync(userId, cancellationTokenInner);
            if (cart is null)
            {
                return null;
            }

            cart.ClearItems();
            await dbContext.SaveChangesAsync(cancellationTokenInner);
            return cart;
        }, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCartExistsAsync(string userId, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Carts\" (\"UserId\") VALUES ({userId}) ON CONFLICT (\"UserId\") DO NOTHING;",
            cancellationToken);
    }

    private Task<Cart?> GetLockedCartAsync(string cartId, string userId, CancellationToken cancellationToken)
    {
        return dbContext.Carts
            .FromSqlInterpolated($"SELECT * FROM \"Carts\" WHERE \"Id\" = {cartId} AND \"UserId\" = {userId} FOR UPDATE")
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private Task<Cart?> GetLockedCartByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return dbContext.Carts
            .FromSqlInterpolated($"SELECT * FROM \"Carts\" WHERE \"UserId\" = {userId} FOR UPDATE")
            .Include(cart => cart.Items)
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
