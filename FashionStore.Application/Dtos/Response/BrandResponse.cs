namespace FashionStore.Application.Dtos.Response;

public sealed class BrandResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public string? WebsiteUrl { get; init; }
    public bool IsActive { get; init; }
    public bool HasImage { get; init; }
    public string? ImageUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record BrandImageResponse(byte[] Data, string ContentType, string FileName);
