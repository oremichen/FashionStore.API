using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Entities;
using FashionStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.OrderRepo;

public sealed class OrderRepository : IOrderRepository
{
    private readonly FashionStoreDbContext _dbContext;

    public OrderRepository(FashionStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> AddressBelongsToUserAsync(string addressId, string userId, CancellationToken cancellationToken)
    {
        return _dbContext.Addresses.AnyAsync(item => item.Id == addressId && item.UserId == userId, cancellationToken);
    }

    public Task<Order?> GetByPaymentReferenceAsync(string reference, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders.Include(item => item.Items).Include(item => item.InventoryReservations);
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.PaymentReference == reference, cancellationToken);
    }

    public Task<Order?> GetByIdempotencyKeyAsync(string userId, string idempotencyKey, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders.Include(item => item.Items).Include(item => item.InventoryReservations);
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.UserId == userId && item.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryReservation>> GetExpiredReservationsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryReservations.Where(item =>
            item.Status == FashionStore.Domain.Constants.InventoryReservationStatuses.Reserved && item.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
