namespace FashionStore.API.Features.Wishlists.Shared;

public sealed class WishlistItemResponse
{
    public required string Id { get; init; }
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string ProductSlug { get; init; }
    public decimal ProductPrice { get; init; }
    public required string CurrencyCode { get; init; }
    public DateTimeOffset AddedAt { get; init; }
}

public sealed class WishlistResponse
{
    public IReadOnlyList<WishlistItemResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class WishlistItemMutationResponse
{
    public required string WishlistItemId { get; init; }
    public required string ProductId { get; init; }
    public bool IsWishlistItem { get; init; }
}
