using FashionStore.Domain.Entities;

namespace FashionStore.API.Features.Carts.Shared;

public static class CartResponseMapper
{
    public static CartResponse Map(Cart cart)
    {
        var items = cart.Items
            .OrderBy(item => item.CreatedAt)
            .Select(item => new CartItemResponse
            {
                Id = item.Id,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ColorId = item.ColorId,
                Quantity = item.Quantity
            })
            .ToList();

        return new CartResponse
        {
            Id = cart.Id,
            Items = items,
            ItemCount = items.Count,
            TotalQuantity = items.Sum(item => item.Quantity),
            UpdatedAt = cart.UpdatedAt
        };
    }
}
