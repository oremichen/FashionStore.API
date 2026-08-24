using FashionStore.Domain.Abstractions.Categories;

namespace FashionStore.API.Features.Categories.GetCategoryById;
public sealed class GetCategoryByIdService(ICategoryRepository repository, ILogger<GetCategoryByIdService> logger) : IGetCategoryByIdService
{
    public async Task<ResponseResult<CategoryDetailsResponse>> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving category {CategoryId}.", id);
        var response = new ResponseResult<CategoryDetailsResponse>();
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogError("Category retrieval validation failed because category id is empty.");
            return response.Fail("Category id is required.", ResponseCodes.INVALID_ACTION);
        }

        var category = await repository.GetByIdAsync(id.Trim(), cancellationToken);
        return category is null ? response.Fail("Category was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : response.Success(ToDetails(category), "Category retrieved successfully.");
    }

    private static CategoryDetailsResponse ToDetails(Category category)
    {
        return new CategoryDetailsResponse
        {
            Id = category.Id,
            ParentId = category.ParentId,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            ShowInMenu = category.ShowInMenu,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
