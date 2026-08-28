namespace FashionStore.API.Features.PromotionVideos.Shared;

public sealed class PromotionVideoResponse
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? VideoUrl { get; init; }
    public bool IsActive { get; init; }
    public bool HasExpired { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
