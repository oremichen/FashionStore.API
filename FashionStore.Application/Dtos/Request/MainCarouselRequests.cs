using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FashionStore.Application.Dtos.Request;

public class CreateMainCarouselRequest
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

public sealed class UpdateMainCarouselForm
{
    [StringLength(150)] public string? Title { get; init; }
    [StringLength(250)] public string? Subtitle { get; init; }
    [StringLength(80)] public string ButtonText { get; init; } = "Shop now";
    [StringLength(2048)] public string? LinkUrl { get; init; }
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public IFormFile? Image { get; init; }
}
