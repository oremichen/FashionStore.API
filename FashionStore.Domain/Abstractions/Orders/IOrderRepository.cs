using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Orders;

public interface IOrderRepository
{
    Task<bool> AddressBelongsToUserAsync(string addressId, string userId, CancellationToken cancellationToken);
    Task<Order?> GetByPaymentReferenceAsync(string reference, bool trackChanges, CancellationToken cancellationToken);
    Task<Order?> GetByIdempotencyKeyAsync(string userId, string idempotencyKey, bool trackChanges, CancellationToken cancellationToken);
    Task<(Order Order, Address? Address, ApplicationUser? User)> GetByPaymentReferenceWithDetailsAsync(string reference, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryReservation>> GetExpiredReservationsAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task CreateWithInventoryReservationsAsync(Order order, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<bool> ReleaseInventoryReservationAsync(string reservationId, string status, CancellationToken cancellationToken);
    Task<bool> ConsumeInventoryReservationAsync(string reservationId, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<IOrderTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task<TResult> ExecuteInRetriableTransactionAsync<TResult>(
        Func<IOrderTransaction, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}

public interface IOrderTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
