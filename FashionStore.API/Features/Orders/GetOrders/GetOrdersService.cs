using FashionStore.API.Features.Orders.Shared;
using FashionStore.Infrastructure.Data;
using FashionStore.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.API.Features.Orders.GetOrders;

public sealed class GetOrdersService(FashionStoreDbContext db) : IGetOrdersService
{
    public async Task<ResponseResult<PagedResponse<OrderResponse>>> ExecuteAsync(string? userId, OrderQuery query, bool admin, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PagedResponse<OrderResponse>>();
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return response.Fail("Page must be at least 1 and pageSize must be between 1 and 100.", ResponseCodes.INVALID_ACTION);

        var orders = admin ? db.Orders.AsNoTracking() : db.Orders.AsNoTracking().Where(item => item.UserId == userId);
        if (!string.IsNullOrWhiteSpace(query.Status)) orders = orders.Where(item => item.Status == query.Status);
        if (query.From.HasValue) orders = orders.Where(item => item.CreatedAt >= query.From.Value);
        if (query.To.HasValue) orders = orders.Where(item => item.CreatedAt <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            orders = orders.Where(item => item.TrackOrderId.ToLower().Contains(search) || item.Email.ToLower().Contains(search) || item.User.FirstName.ToLower().Contains(search) || item.User.LastName.ToLower().Contains(search));
        }

        var totalCount = await orders.CountAsync(cancellationToken);
        var items = await orders.Include("User").Include("Items").OrderByDescending(item => item.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        var productIds = items.SelectMany(order => order.Items).Select(item => item.ProductId).Distinct().ToList();
        var productImages = await (from product in db.Products
                                   from image in product.Images
                                   where productIds.Contains(product.Id) && image.IsPrimary
                                   select new { product.Id, image.SmallUrl, image.MediumUrl, image.BigUrl })
            .ToDictionaryAsync(item => item.Id, item => item.SmallUrl ?? item.MediumUrl ?? item.BigUrl, cancellationToken);
        var addresses = await db.Addresses.AsNoTracking()
            .Where(address => items.Select(order => order.AddressId).Contains(address.Id))
            .ToDictionaryAsync(address => address.Id, cancellationToken);

        return response.Success(new PagedResponse<OrderResponse>
        {
            Items = items.Select(order => OrderResponseMapper.Map(order,
                addresses.TryGetValue(order.AddressId, out var address) ? UserAddressResponse.From(address) : null,
                productImages)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        }, "Orders retrieved successfully.");
    }
}
