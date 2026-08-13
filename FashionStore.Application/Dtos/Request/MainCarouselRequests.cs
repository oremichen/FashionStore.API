using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FashionStore.Application.Dtos.Request;

public sealed record CreateMainCarouselRequest(string Title, string? Subtitle, string ButtonText, string? LinkUrl,
    int SortOrder, bool IsActive, byte[] ImageData, string ImageContentType, string ImageFileName, int ImageWidth, int ImageHeight);

public sealed record UpdateMainCarouselRequest(string Title, string? Subtitle, string ButtonText, string? LinkUrl,
    int SortOrder, bool IsActive, byte[]? ImageData, string? ImageContentType, string? ImageFileName, int? ImageWidth, int? ImageHeight);

public sealed class CreateMainCarouselForm
{
    [Required, StringLength(150)] public string Title { get; init; } = string.Empty;
    [StringLength(250)] public string? Subtitle { get; init; }
    [Required, StringLength(80)] public string ButtonText { get; init; } = "Shop now";
    [StringLength(2048)] public string? LinkUrl { get; init; }
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    [Required] public IFormFile Image { get; init; } = null!;
    [Range(1280, 3840)] public int ImageWidth { get; init; }
    [Range(500, 2160)] public int ImageHeight { get; init; }
}

public sealed class UpdateMainCarouselForm
{
    [Required, StringLength(150)] public string Title { get; init; } = string.Empty;
    [StringLength(250)] public string? Subtitle { get; init; }
    [Required, StringLength(80)] public string ButtonText { get; init; } = "Shop now";
    [StringLength(2048)] public string? LinkUrl { get; init; }
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public IFormFile? Image { get; init; }
    [Range(1280, 3840)] public int? ImageWidth { get; init; }
    [Range(500, 2160)] public int? ImageHeight { get; init; }
}
