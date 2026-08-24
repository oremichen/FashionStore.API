namespace FashionStore.API.Features.MainCarousels.UpdateMainCarousel;

public sealed class UpdateMainCarouselRequest
{
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
    public string? ButtonText { get; init; }
    public string? LinkUrl { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public byte[]? ImageData { get; init; }
    public string? ImageContentType { get; init; }
    public string? ImageFileName { get; init; }
}
