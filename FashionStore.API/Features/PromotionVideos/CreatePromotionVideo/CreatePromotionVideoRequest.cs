namespace FashionStore.API.Features.PromotionVideos.CreatePromotionVideo;

public sealed record CreatePromotionVideoRequest(string Title, string Slug, bool IsActive, DateTimeOffset? ExpiresAt,
    byte[] VideoData, string VideoContentType, string VideoFileName);
