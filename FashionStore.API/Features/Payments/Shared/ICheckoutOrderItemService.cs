using FashionStore.Domain.Entities;

namespace FashionStore.API.Features.Payments.Shared;

public interface ICheckoutOrderItemService
{
    Task<List<OrderItem>?> BuildAsync(IEnumerable<CheckoutItemRequest> requestedItems, string userId, CancellationToken cancellationToken);
}
