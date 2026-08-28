using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Categories;

public sealed record CategoryListItem(string Id, string Name, string Slug, string? ParentId, bool HasSubCategory);

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryListItem>> GetPublicCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryListItem>> GetCategoriesWithParentAsync(CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken, string? excludedId = null);
    Task<bool> NameExistsUnderParentAsync(string name, string? parentId, CancellationToken cancellationToken, string? excludedId = null);
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
