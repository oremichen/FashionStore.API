using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Sizes.CreateSize;

public sealed class CreateSizeRequest
{
    [Required, StringLength(50)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(100)] public string DisplayName { get; init; } = string.Empty;
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
}
