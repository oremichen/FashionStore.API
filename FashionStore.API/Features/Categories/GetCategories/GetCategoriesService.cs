using FashionStore.Domain.Abstractions.Categories;

namespace FashionStore.API.Features.Categories.GetCategories;
public sealed class GetCategoriesService(ICategoryRepository repository, ILogger<GetCategoriesService> logger) : IGetCategoriesService
{
    public async Task<ResponseResult<IReadOnlyList<CategoryResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving public categories.");
        return new ResponseResult<IReadOnlyList<CategoryResponse>>().Success((await repository.GetPublicCategoriesAsync(cancellationToken)).Select(ToResponse).ToList(), "Categories retrieved successfully.");
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
