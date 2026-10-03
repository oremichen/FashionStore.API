using FashionStore.Domain.Abstractions.Categories;

namespace FashionStore.API.Features.Categories.GetAllCategories;

public sealed class GetAllCategoriesService(
    ICategoryRepository repository,
    ILogger<GetAllCategoriesService> logger) : IGetAllCategoriesService
{
    public async Task<ResponseResult<IReadOnlyList<CategoryResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving all categories for administration.");

        var categories = await repository.GetAllCategoriesAsync(cancellationToken);
        var response = categories.Select(ToResponse).ToList();

        logger.LogInformation("Retrieved {CategoryCount} categories for administration.", response.Count);
        return new ResponseResult<IReadOnlyList<CategoryResponse>>()
            .Success(response, "All categories retrieved successfully.");
    }

    private static CategoryResponse ToResponse(CategoryListItem category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentId = category.ParentId,
            HasSubCategory = category.HasSubCategory
        };
    }
}
