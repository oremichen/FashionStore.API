using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Categories.CreateCategory;

public sealed class CreateCategoryRequest
{
    public string? ParentId { get; init; }

    [Required, StringLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(180)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }
    [Range(0, int.MaxValue)] public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public bool ShowInMenu { get; init; } = true;
}
