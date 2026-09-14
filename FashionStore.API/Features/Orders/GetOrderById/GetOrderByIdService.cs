using FashionStore.API.Features.Orders.Shared;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Shared.Constants;

namespace FashionStore.API.Features.Orders.GetOrderById;

public sealed class GetOrderByIdService(IOrderRepository orderRepository) : IGetOrderByIdService
{
    public async Task<ResponseResult<OrderResponse>> ExecuteAsync(string id, string? userId, bool admin, CancellationToken cancellationToken)
    {
        var result = await orderRepository.GetOrderByIdOrTrackIdWithResponseDetailsAsync(id, userId, admin, cancellationToken);
        if (result is null)
            return new ResponseResult<OrderResponse>().Fail("Order was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var (order, address, productImages) = result.Value;
        return new ResponseResult<OrderResponse>().Success(
            OrderResponseMapper.Map(order, address is null ? null : UserAddressResponse.From(address), productImages),
            "Order retrieved successfully.");
    }
}
