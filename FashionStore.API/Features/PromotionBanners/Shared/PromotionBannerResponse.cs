namespace FashionStore.API.Features.PromotionBanners.Shared;

public sealed class PromotionBannerResponse
{
    public required string Id { get; init; }
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
    public required string Image { get; init; }
    public string? DestinationUrl { get; init; }
    public required string Placement { get; init; }
    public int Slot { get; init; }
    public bool IsActive { get; init; }
}
