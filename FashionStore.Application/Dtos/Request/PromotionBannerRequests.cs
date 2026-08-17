using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FashionStore.Application.Dtos.Request;

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

public sealed class UpdatePromotionBannerForm
{
    [StringLength(150)] public string? Title { get; init; }
    [StringLength(250)] public string? Subtitle { get; init; }
    [StringLength(2048)] public string? DestinationUrl { get; init; }
    [StringLength(100)] public string? Placement { get; init; } = "homepage-banner-grid";
    [Range(1, int.MaxValue)] public int Slot { get; init; }
    public bool? IsActive { get; init; }
    public IFormFile? Image { get; init; }
}

public sealed record CreatePromotionBannerRequest(string? Title, string? Subtitle, string? DestinationUrl,
    string? Placement, int Slot, bool IsActive, byte[] ImageData, string ImageContentType, string ImageFileName);

public sealed record UpdatePromotionBannerRequest(string? Title, string? Subtitle, string? DestinationUrl,
    string? Placement, int Slot, bool IsActive, byte[]? ImageData, string? ImageContentType, string? ImageFileName);
