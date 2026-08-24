using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.MainCarousels.CreateMainCarousel;

public sealed class CreateMainCarouselForm
{
    [StringLength(150)] public string? Title { get; init; }
    [StringLength(250)] public string? Subtitle { get; init; }
    [StringLength(80)] public string ButtonText { get; init; } = "Shop now";
    [StringLength(2048)] public string? LinkUrl { get; init; }
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    [Required] public IFormFile Image { get; init; } = null!;
}
