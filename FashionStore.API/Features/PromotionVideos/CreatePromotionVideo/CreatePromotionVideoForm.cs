using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.PromotionVideos.CreatePromotionVideo;

public sealed class CreatePromotionVideoForm
{
    [Required, StringLength(150)] public string Title { get; init; } = string.Empty;
    [Required, StringLength(180)] public string Slug { get; init; } = string.Empty;
    public bool? IsActive { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    [Required] public IFormFile Video { get; init; } = null!;
}
