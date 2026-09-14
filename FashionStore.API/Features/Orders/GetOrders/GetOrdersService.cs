using FashionStore.API.Features.Orders.Shared;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Shared.Constants;

namespace FashionStore.API.Features.Orders.GetOrders;

public sealed class GetOrdersService(IOrderRepository orderRepository) : IGetOrdersService
{
    public async Task<ResponseResult<PagedResponse<OrderResponse>>> ExecuteAsync(string? userId, OrderQuery query, bool admin, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PagedResponse<OrderResponse>>();
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return response.Fail("Page must be at least 1 and pageSize must be between 1 and 100.", ResponseCodes.INVALID_ACTION);

        var result = await orderRepository.GetPagedOrdersAsync(
            userId,
            query.Page,
            query.PageSize,
            admin,
            query.Status,
            query.Search,
            query.From,
            query.To,
            cancellationToken);

        return response.Success(new PagedResponse<OrderResponse>
        {
            Items = result.Items.Select(order => OrderResponseMapper.Map(order,
                result.Addresses.TryGetValue(order.AddressId, out var address) ? UserAddressResponse.From(address) : null,
                result.ProductImages)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)query.PageSize)
        }, "Orders retrieved successfully.");
    }
}
