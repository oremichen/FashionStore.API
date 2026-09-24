using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Types.CreateType;

public sealed class CreateTypeRequest
{
    [Required, StringLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(150)]
    public string Slug { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}
