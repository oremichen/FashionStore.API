namespace FashionStore.API.Features.Orders.Shared;

public sealed class OrderQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Status { get; init; }
    public string? Search { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed class OrderItemResponse
{
    public required string Id { get; init; }
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? ProductImageUrl { get; init; }
    public string? VariantId { get; init; }
    public string? ColorId { get; init; }
    public string? ColorName { get; init; }
    public string? SizeName { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class OrderResponse
{
    public required string Id { get; init; }
    public required string Number { get; init; }
    public required string UserId { get; init; }
    public required string Customer { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public UserAddressResponse? Address { get; init; }
    public required string Status { get; init; }
    public string? RefundedReason { get; init; }
    public required string PaymentStatus { get; init; }
    public required string Currency { get; init; }
    public required string DeliveryMethod { get; init; }
    public DateTimeOffset Date { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Shipping { get; init; }
    public decimal Total { get; init; }
    public IReadOnlyList<OrderItemResponse> Items { get; init; } = [];
}

public sealed class UpdateOrderStatusRequest
{
    public string Status { get; init; } = string.Empty;
    public string? RefundedReason { get; init; }
}

public static class OrderResponseMapper
{
    public static OrderResponse Map(FashionStore.Domain.Entities.Order order, UserAddressResponse? address = null, IReadOnlyDictionary<string, string?>? productImages = null)
    {
        return new OrderResponse
        {
            Id = order.Id,
            Number = order.TrackOrderId,
            UserId = order.UserId,
            Customer = $"{order.User.FirstName} {order.User.LastName}".Trim(),
            Username = order.User.UserName ?? order.Email,
            Email = order.Email,
            Phone = address?.PhoneNumber,
            Address = address,
            Status = order.Status,
            RefundedReason = order.RefundedReason,
            PaymentStatus = order.PaymentStatus,
            Currency = order.Currency,
            DeliveryMethod = order.DeliveryMethod,
            Date = order.CreatedAt,
            Subtotal = order.Subtotal,
            Shipping = order.DeliveryFee,
            Total = order.Total,
            Items = order.Items.Select(item => MapItem(item, productImages)).ToList()
        };
    }

    private static OrderItemResponse MapItem(FashionStore.Domain.Entities.OrderItem item, IReadOnlyDictionary<string, string?>? productImages)
    {
        return new OrderItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ProductImageUrl = productImages is not null && productImages.TryGetValue(item.ProductId, out var imageUrl) ? imageUrl : null,
            VariantId = item.VariantId,
            ColorId = item.ColorId,
            ColorName = item.ColorName,
            SizeName = item.SizeName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            LineTotal = item.LineTotal
        };
    }
}
