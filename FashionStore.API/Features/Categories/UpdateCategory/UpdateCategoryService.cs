using FashionStore.Domain.Abstractions.Categories;

namespace FashionStore.API.Features.Categories.UpdateCategory;
public sealed class UpdateCategoryService(ICategoryRepository repository, ILogger<UpdateCategoryService> logger) : IUpdateCategoryService
{
    public async Task<ResponseResult<CategoryDetailsResponse>> ExecuteAsync(string id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating category {CategoryId}.", id);
        var response = new ResponseResult<CategoryDetailsResponse>();
        if (string.IsNullOrWhiteSpace(id))
            return response.Fail("Category id is required.", ResponseCodes.INVALID_ACTION);
        var categoryId = id.Trim();
        var category = await repository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return response.Fail("Category was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        var parentId = string.IsNullOrWhiteSpace(request.ParentId) ? null : request.ParentId.Trim();
        if (parentId == categoryId)
            return response.Fail("A category cannot be its own parent.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        if (parentId is not null && await repository.GetByIdAsync(parentId, cancellationToken)is null)
            return response.Fail("The selected parent category does not exist.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        if (await repository.SlugExistsAsync(request.Slug.Trim(), cancellationToken, categoryId))
            return response.Fail("An active category with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        if (await repository.NameExistsUnderParentAsync(request.Name.Trim(), parentId, cancellationToken, categoryId))
            return response.Fail("A category with this name already exists under the selected parent.", ResponseCodes.DUPLICATE_RECORD);
        try
        {
            category.SetDetails(request.Name, request.Slug, request.Description, request.SortOrder, request.IsActive, request.ShowInMenu);
            category.AssignParent(parentId);
            await repository.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated category {CategoryId}.", categoryId);
            return response.Success(ToDetails(category), "Category updated successfully.");
        }
        catch (ArgumentException exception)
        {
            logger.LogError(exception, "Category update validation failed for {CategoryId}.", categoryId);
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
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
