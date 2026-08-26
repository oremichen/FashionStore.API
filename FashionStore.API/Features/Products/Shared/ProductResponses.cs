namespace FashionStore.API.Features.Products.Shared;

public sealed class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class ProductImageResponse
{
    public required string Id { get; init; }
    public string? SmallUrl { get; init; }
    public string? MediumUrl { get; init; }
    public string? BigUrl { get; init; }
    public string? AlternativeText { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
public sealed class SizeResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}
public sealed class ColorResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? HexCode { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}
public class ProductResponse
{
    public required string Id { get; init; }
    public required string CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public string? BrandId { get; init; }
    public string? BrandName { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? AdditionalInformation { get; init; }
    public string? ShortDescription { get; init; }
    public decimal? OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool HasPriceRange { get; init; }
    public decimal? Discount { get; init; }
    public required string CurrencyCode { get; init; }
    public int AvailabilityCount { get; init; }
    public int ColorCount { get; init; }
    public required string StockStatus { get; init; }
    public decimal? Weight { get; init; }
    public string? WeightUnit { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsNewArrival { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int Star { get; init; }
    public string? Ratings { get; init; }
    public required IReadOnlyList<ProductImageResponse> Images { get; init; }
    public required IReadOnlyList<ColorResponse> Colors { get; init; }
    public required IReadOnlyList<SizeResponse> Sizes { get; init; }
}

public sealed class ProductDetailResponse : ProductResponse
{
    public required IReadOnlyList<ProductVariantResponse> ProductVariants { get; init; }
}

public sealed class ProductVariantResponse
{
    public required string Id { get; init; }
    public string? SizeId { get; init; }
    public string? Size { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
}
