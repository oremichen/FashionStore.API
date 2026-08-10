using FashionStore.Application.Abstractions.Categories;
using FashionStore.Application.Dtos.Response;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.CategoryRepo;

public sealed class CategoryRepository(FashionStoreDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryResponse>> GetPublicCategoriesAsync(CancellationToken cancellationToken)
        => await Project(dbContext.Categories.Where(category => category.DeletedAt == null && category.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesWithParentAsync(CancellationToken cancellationToken)
        => await Project(dbContext.Categories.Where(category => category.DeletedAt == null && category.ParentId != null))
            .ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken)
        => dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id && category.DeletedAt == null, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        => dbContext.Categories.AnyAsync(category => category.DeletedAt == null && category.Slug.ToLower() == slug.ToLower(), cancellationToken);

    public Task<bool> NameExistsUnderParentAsync(string name, string? parentId, CancellationToken cancellationToken)
        => dbContext.Categories.AnyAsync(category => category.DeletedAt == null
            && category.ParentId == parentId && category.Name.ToLower() == name.ToLower(), cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<CategoryResponse> Project(IQueryable<Category> categories)
        => categories.AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.ParentId,
                category.Children.Any(child => child.DeletedAt == null && child.IsActive)));
}
