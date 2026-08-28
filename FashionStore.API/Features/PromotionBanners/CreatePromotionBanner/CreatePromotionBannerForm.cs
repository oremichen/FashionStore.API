using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.PromotionBanners.CreatePromotionBanner;

public sealed class CreatePromotionBannerForm
{
    [StringLength(150)] public string? Title { get; init; }
    [StringLength(250)] public string? Subtitle { get; init; }
    [StringLength(2048)] public string? DestinationUrl { get; init; }
    [StringLength(100)] public string? Placement { get; init; } = "homepage-banner-grid";
    [Range(1, int.MaxValue)] public int Slot { get; init; }
    public bool? IsActive { get; init; }
    [Required] public IFormFile Image { get; init; } = null!;
}
