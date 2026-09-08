using FashionStore.API.Features.Orders.Shared;

namespace FashionStore.API.Features.Orders.GetOrderById;

public interface IGetOrderByIdService
{
    Task<ResponseResult<OrderResponse>> ExecuteAsync(string id, string? userId, bool admin, CancellationToken cancellationToken);
}
