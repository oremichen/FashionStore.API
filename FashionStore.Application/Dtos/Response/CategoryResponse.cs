namespace FashionStore.Application.Dtos.Response;

public sealed record CategoryResponse(string Id, string Name, string Slug, string? ParentId, bool HasSubCategory);

public sealed record CategoryDetailsResponse(
    string Id,
    string? ParentId,
    string Name,
    string Slug,
    string? Description,
    int SortOrder,
    bool IsActive,
    bool ShowInMenu,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
