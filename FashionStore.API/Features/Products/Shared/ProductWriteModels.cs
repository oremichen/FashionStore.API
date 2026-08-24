using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Products.Shared;

public abstract class ProductWriteRequest
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
    public string? Sizes { get; init; }
    public string? Colors { get; init; }
    public string Status { get; init; } = "draft";
}

public sealed record ProductImageRequest(byte[] Data, string ContentType, string FileName);
