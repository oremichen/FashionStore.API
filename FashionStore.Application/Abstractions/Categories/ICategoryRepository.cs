namespace FashionStore.Application.Abstractions.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryResponse>> GetPublicCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesWithParentAsync(CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
    Task<bool> NameExistsUnderParentAsync(string name, string? parentId, CancellationToken cancellationToken);
    Task AddAsync(Category category, CancellationToken cancellationToken);
}
