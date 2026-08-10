namespace FashionStore.Application.Dtos.Request;

public sealed record CreateBrandRequest(string Name, string Slug, string? Description, string? WebsiteUrl,
    bool IsActive, byte[]? ImageData, string? ImageContentType, string? ImageFileName);
