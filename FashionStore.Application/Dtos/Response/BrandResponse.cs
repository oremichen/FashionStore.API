namespace FashionStore.Application.Dtos.Response;

public sealed record BrandResponse(string Id, string Name, string Slug, string? Description, string? WebsiteUrl,
    bool IsActive, bool HasImage, string? ImageUrl, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record BrandImageResponse(byte[] Data, string ContentType, string FileName);
