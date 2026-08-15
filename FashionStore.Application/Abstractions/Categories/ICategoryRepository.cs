namespace FashionStore.Application.Abstractions.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryResponse>> GetPublicCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesWithParentAsync(CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken, string? excludedId = null);
    Task<bool> NameExistsUnderParentAsync(string name, string? parentId, CancellationToken cancellationToken, string? excludedId = null);
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
