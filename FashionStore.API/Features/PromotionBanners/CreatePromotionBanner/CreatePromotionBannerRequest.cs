namespace FashionStore.API.Features.PromotionBanners.CreatePromotionBanner;

public sealed record CreatePromotionBannerRequest(string? Title, string? Subtitle, string? DestinationUrl,
    string? Placement, int Slot, bool IsActive, byte[] ImageData, string ImageContentType, string ImageFileName);
