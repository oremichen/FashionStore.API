using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FashionStore.Application.Dtos.Request;

public sealed class PromotionVideoQuery
{
    [StringLength(180)] public string? Search { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 25;
}

public sealed class CreatePromotionVideoForm
{
    [Required, StringLength(150)] public string Title { get; init; } = string.Empty;
    [Required, StringLength(180)] public string Slug { get; init; } = string.Empty;
    public bool? IsActive { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    [Required] public IFormFile Video { get; init; } = null!;
}

public sealed class UpdatePromotionVideoRequest
{
    [Required, StringLength(150)] public string Title { get; init; } = string.Empty;
    [Required, StringLength(180)] public string Slug { get; init; } = string.Empty;
    public bool? IsActive { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record CreatePromotionVideoRequest(string Title, string Slug, bool IsActive, DateTimeOffset? ExpiresAt,
    byte[] VideoData, string VideoContentType, string VideoFileName);
