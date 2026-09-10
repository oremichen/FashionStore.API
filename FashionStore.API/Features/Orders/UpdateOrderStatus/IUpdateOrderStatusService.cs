using FashionStore.API.Features.Orders.Shared;

namespace FashionStore.API.Features.Orders.UpdateOrderStatus;

public interface IUpdateOrderStatusService
{
    Task<ResponseResult<OrderResponse>> ExecuteAsync(string id, UpdateOrderStatusRequest request, CancellationToken cancellationToken);
}
