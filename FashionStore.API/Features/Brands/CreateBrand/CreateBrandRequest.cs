namespace FashionStore.API.Features.Brands.CreateBrand;

public sealed class CreateBrandRequest
{
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? WebsiteUrl { get; init; }
    public bool IsActive { get; init; }
    public byte[]? ImageData { get; init; }
    public string? ImageContentType { get; init; }
    public string? ImageFileName { get; init; }
}
