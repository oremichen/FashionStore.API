namespace FashionStore.Application.Dtos.Response;

public sealed class CategoryResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? ParentId { get; init; }
    public bool HasSubCategory { get; init; }
}

public sealed class CategoryDetailsResponse
{
    public required string Id { get; init; }
    public string? ParentId { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool ShowInMenu { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
