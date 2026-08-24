using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Colors.CreateColor;

public sealed class CreateColorRequest
{
    [Required, StringLength(100)] public string Name { get; init; } = string.Empty;
    [StringLength(9)] public string? HexCode { get; init; }
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
}
