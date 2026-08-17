namespace FashionStore.Application.Dtos.Response;

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

public sealed record PromotionBannerImageResponse(byte[] Data, string ContentType);
