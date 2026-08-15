using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FashionStore.Application.Dtos.Request;

public sealed class ProductQuery
{
    public string? Search { get; init; }
    public string? CategoryId { get; init; }
    public string? BrandId { get; init; }
    public string? Status { get; init; }
    public string? StockStatus { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string Sort { get; init; } = "newest";
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 25;
    [Range(1, int.MaxValue)] public int LowStockThreshold { get; init; } = 5;
}

public sealed class StorefrontProductQuery
{
    public string? Search { get; init; }
    public string? CategorySlug { get; init; }
    public bool IncludeDescendants { get; init; }
    public string? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Colors { get; init; }
    public string? Sizes { get; init; }
    public bool? InStock { get; init; }
    [Range(0, 5)] public decimal? MinRating { get; init; }
    public string Sort { get; init; } = "newest";
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 24;
}

public class ProductRequest
{
    [Required] public string CategoryId { get; init; } = string.Empty;
    public string? BrandId { get; init; }
    [Required, StringLength(250)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(280)] public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? AdditionalInformation { get; init; }
    [StringLength(500)] public string? ShortDescription { get; init; }
    public decimal? OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    [StringLength(3, MinimumLength = 3)] public string CurrencyCode { get; init; } = "NGN";
    public int AvailabilityCount { get; init; }
    public decimal? Weight { get; init; }
    public string? WeightUnit { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsNewArrival { get; init; }
    public string Status { get; init; } = "draft";
}

public class CreateProductRequest : ProductRequest
{
    public required IReadOnlyList<ProductImageRequest> ImageRequests { get; init; }
}

public sealed class UpdateProductRequest : CreateProductRequest
{
    [Required] public required string ProductId { get; init; }
}

public sealed record ProductImageRequest(byte[] Data, string ContentType, string FileName);

public class ProductForm : ProductRequest
{
    public List<IFormFile> Images { get; init; } = [];
}

public sealed class CreateProductForm : ProductForm { }

public sealed class UpdateProductForm : ProductForm
{
    [Required] public string ProductId { get; init; } = string.Empty;
}
