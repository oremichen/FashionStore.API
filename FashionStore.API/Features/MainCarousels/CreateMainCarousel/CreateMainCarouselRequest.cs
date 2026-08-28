namespace FashionStore.API.Features.MainCarousels.CreateMainCarousel;

public sealed class CreateMainCarouselRequest
{
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
    public string? ButtonText { get; init; }
    public string? LinkUrl { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public required byte[] ImageData { get; init; }
    public required string ImageContentType { get; init; }
    public required string ImageFileName { get; init; }
}
