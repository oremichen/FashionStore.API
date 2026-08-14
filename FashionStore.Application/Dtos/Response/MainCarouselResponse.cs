namespace FashionStore.Application.Dtos.Response;

public sealed record MainCarouselResponse(
    string Id,
    string? Title,
    string? Subtitle,
    string? ButtonText,
    string? LinkUrl,
    int SortOrder,
    bool IsActive,
    bool HasImage,
    string? ImageUrl,
    string Image,
    int? ImageWidth,
    int? ImageHeight,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record MainCarouselImageResponse(byte[] Data, string ContentType, string? FileName);
