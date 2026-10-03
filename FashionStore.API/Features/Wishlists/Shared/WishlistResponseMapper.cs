using FashionStore.Domain.Entities;

namespace FashionStore.API.Features.Wishlists.Shared;

public static class WishlistResponseMapper
{
    public static WishlistItemResponse Map(WishlistItem item)
    {
        return new WishlistItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product.Name,
            ProductSlug = item.Product.Slug,
            ProductPrice = item.Product.NewPrice,
            CurrencyCode = item.Product.CurrencyCode,
            AddedAt = item.CreatedAt
        };
    }
}
