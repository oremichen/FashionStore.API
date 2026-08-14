namespace FashionStore.Application.Dtos.Response;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record ProductImageResponse(string Id, string? SmallUrl, string? MediumUrl, string? BigUrl,
    string? AlternativeText, int SortOrder, bool IsPrimary, DateTimeOffset CreatedAt);
public sealed record ProductImageFileResponse(byte[] Data, string ContentType, string FileName);
public sealed record ProductResponse(string Id, string CategoryId, string CategoryName, string? BrandId,
    string? BrandName, string Name, string Slug, string? Description, string? ShortDescription, decimal? OldPrice,
    decimal NewPrice, decimal? Discount, string CurrencyCode, int AvailabilityCount, string StockStatus,
    decimal? Weight, string? WeightUnit, bool IsFeatured, bool IsNewArrival, string Status,
    DateTimeOffset? PublishedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<ProductImageResponse> Images);
