using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.PromotionVideos.UpdatePromotionVideo;

public sealed class UpdatePromotionVideoRequest
{
    [Required, StringLength(150)] public string Title { get; init; } = string.Empty;
    [Required, StringLength(180)] public string Slug { get; init; } = string.Empty;
    public bool? IsActive { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
