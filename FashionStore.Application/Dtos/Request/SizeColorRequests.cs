using System.ComponentModel.DataAnnotations;

namespace FashionStore.Application.Dtos.Request;

public sealed class CreateSizeRequest
{
    [Required, StringLength(50)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(100)] public string DisplayName { get; init; } = string.Empty;
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
}

public sealed class CreateColorRequest
{
    [Required, StringLength(100)] public string Name { get; init; } = string.Empty;
    [StringLength(9)] public string? HexCode { get; init; }
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
}
