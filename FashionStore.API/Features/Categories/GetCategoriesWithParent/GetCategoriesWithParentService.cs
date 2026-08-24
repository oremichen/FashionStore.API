using FashionStore.Domain.Abstractions.Categories;

namespace FashionStore.API.Features.Categories.GetCategoriesWithParent;
public sealed class GetCategoriesWithParentService(ICategoryRepository repository, ILogger<GetCategoriesWithParentService> logger) : IGetCategoriesWithParentService
{
    public async Task<ResponseResult<IReadOnlyList<CategoryResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving categories with parents.");
        return new ResponseResult<IReadOnlyList<CategoryResponse>>().Success((await repository.GetCategoriesWithParentAsync(cancellationToken)).Select(ToResponse).ToList(), "Categories with a parent retrieved successfully.");
    }

    private static CategoryResponse ToResponse(CategoryListItem category)
    {
        return new()
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentId = category.ParentId,
            HasSubCategory = category.HasSubCategory
        };
    }
}
