namespace FashionStore.API.Features.PromotionBanners.UpdatePromotionBanner;

public sealed record UpdatePromotionBannerRequest(string? Title, string? Subtitle, string? DestinationUrl,
    string? Placement, int Slot, bool IsActive, byte[]? ImageData, string? ImageContentType, string? ImageFileName);
