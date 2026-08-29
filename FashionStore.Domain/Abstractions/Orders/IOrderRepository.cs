using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Orders;

public interface IOrderRepository
{
    Task<bool> AddressBelongsToUserAsync(string addressId, string userId, CancellationToken cancellationToken);
    Task<Order?> GetByPaymentReferenceAsync(string reference, bool trackChanges, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
