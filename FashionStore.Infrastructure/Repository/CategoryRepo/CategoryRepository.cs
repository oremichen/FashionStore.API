using FashionStore.Application.Abstractions.Categories;
using FashionStore.Application.Dtos.Response;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.CategoryRepo;

public sealed class CategoryRepository(FashionStoreDbContext dbContext, ILogger<CategoryRepository> logger) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryResponse>> GetPublicCategoriesAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying public categories.");
        return await Project(dbContext.Categories.Where(category => category.DeletedAt == null && category.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesWithParentAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying categories with parents.");
        return await Project(dbContext.Categories.Where(category => category.DeletedAt == null && category.ParentId != null))
            .ToListAsync(cancellationToken);
    }

    public Task<Category?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying category {CategoryId}.", id);
        return dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id && category.DeletedAt == null, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking category slug {Slug}.", slug);
        return dbContext.Categories.AnyAsync(category => category.DeletedAt == null && category.Slug.ToLower() == slug.ToLower(), cancellationToken);
    }

    public Task<bool> NameExistsUnderParentAsync(string name, string? parentId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking category name under parent {ParentId}.", parentId);
        return dbContext.Categories.AnyAsync(category => category.DeletedAt == null
            && category.ParentId == parentId && category.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        logger.LogInformation("Persisting category {CategoryId}.", category.Id);
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Persisted category {CategoryId}.", category.Id);
    }

    private static IQueryable<CategoryResponse> Project(IQueryable<Category> categories)
    {
        return categories.AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse
            {
                Id = category.Id, Name = category.Name, Slug = category.Slug, ParentId = category.ParentId,
                HasSubCategory = category.Children.Any(child => child.DeletedAt == null && child.IsActive)
            });
    }
}
