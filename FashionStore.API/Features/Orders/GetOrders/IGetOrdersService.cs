using FashionStore.API.Features.Orders.Shared;

namespace FashionStore.API.Features.Orders.GetOrders;

public interface IGetOrdersService
{
    Task<ResponseResult<PagedResponse<OrderResponse>>> ExecuteAsync(string? userId, OrderQuery query, bool admin, CancellationToken cancellationToken);
}
