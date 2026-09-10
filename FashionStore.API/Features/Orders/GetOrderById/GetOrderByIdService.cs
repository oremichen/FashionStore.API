using FashionStore.API.Features.Orders.Shared;
using FashionStore.Infrastructure.Data;
using FashionStore.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.API.Features.Orders.GetOrderById;

public sealed class GetOrderByIdService(FashionStoreDbContext db) : IGetOrderByIdService
{
    public async Task<ResponseResult<OrderResponse>> ExecuteAsync(string id, string? userId, bool admin, CancellationToken cancellationToken)
    {
        var orders = admin ? db.Orders.AsNoTracking() : db.Orders.AsNoTracking().Where(item => item.UserId == userId);
        var order = await orders.Include("User").Include("Items").SingleOrDefaultAsync(item => item.Id == id || item.TrackOrderId == id, cancellationToken);
        if (order is null)
            return new ResponseResult<OrderResponse>().Fail("Order was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var productImages = await (from product in db.Products
                                   from image in product.Images
                                   where productIds.Contains(product.Id) && image.IsPrimary
                                   select new { product.Id, image.SmallUrl, image.MediumUrl, image.BigUrl })
            .ToDictionaryAsync(item => item.Id, item => item.SmallUrl ?? item.MediumUrl ?? item.BigUrl, cancellationToken);

        var address = await db.Addresses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == order.AddressId, cancellationToken);
        return new ResponseResult<OrderResponse>().Success(OrderResponseMapper.Map(order, address is null ? null : UserAddressResponse.From(address), productImages), "Order retrieved successfully.");
    }
}
