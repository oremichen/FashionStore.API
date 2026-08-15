namespace FashionStore.Application.Dtos.Response;

public sealed class MainCarouselResponse
{
    public required string Id { get; init; }
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
    public string? ButtonText { get; init; }
    public string? LinkUrl { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool HasImage { get; init; }
    public string? ImageUrl { get; init; }
    public required string Image { get; init; }
    public int? ImageWidth { get; init; }
    public int? ImageHeight { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record MainCarouselImageResponse(byte[] Data, string ContentType, string? FileName);
