using FashionStore.Application.Abstractions.Categories;

namespace FashionStore.Application.Features.Categories;

public sealed class CategoryService(ICategoryRepository repository, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<ResponseResult<IReadOnlyList<CategoryResponse>>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving public categories.");
        return new ResponseResult<IReadOnlyList<CategoryResponse>>().Success(
            await repository.GetPublicCategoriesAsync(cancellationToken), "Categories retrieved successfully.");
    }

    public async Task<ResponseResult<IReadOnlyList<CategoryResponse>>> GetCategoriesWithParentAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving categories with parents.");
        return new ResponseResult<IReadOnlyList<CategoryResponse>>().Success(
            await repository.GetCategoriesWithParentAsync(cancellationToken), "Categories with a parent retrieved successfully.");
    }

    public async Task<ResponseResult<CategoryDetailsResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving category {CategoryId}.", id);
        var response = new ResponseResult<CategoryDetailsResponse>();
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogError("Category retrieval validation failed because category id is empty.");
            return response.Fail("Category id is required.", ResponseCodes.INVALID_ACTION);
        }

        var category = await repository.GetByIdAsync(id.Trim(), cancellationToken);
        return category is null
            ? response.Fail("Category was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD)
            : response.Success(ToDetails(category), "Category retrieved successfully.");
    }

    public async Task<ResponseResult<CategoryDetailsResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating category with slug {Slug} under parent {ParentId}.", request.Slug, request.ParentId);
        var response = new ResponseResult<CategoryDetailsResponse>();
        var parentId = string.IsNullOrWhiteSpace(request.ParentId) ? null : request.ParentId.Trim();

        if (parentId is not null && await repository.GetByIdAsync(parentId, cancellationToken) is null)
        {
            logger.LogError("Category creation validation failed because parent category {ParentId} does not exist.", parentId);
            return response.Fail("The selected parent category does not exist.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        }
        if (await repository.SlugExistsAsync(request.Slug.Trim(), cancellationToken))
        {
            logger.LogError("Category creation validation failed because slug {Slug} already exists.", request.Slug);
            return response.Fail("An active category with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        }
        if (await repository.NameExistsUnderParentAsync(request.Name.Trim(), parentId, cancellationToken))
        {
            logger.LogError("Category creation validation failed because name {CategoryName} already exists under parent {ParentId}.", request.Name, parentId);
            return response.Fail("A category with this name already exists under the selected parent.", ResponseCodes.DUPLICATE_RECORD);
        }

        try
        {
            var category = Category.Create(request.Name, request.Slug, request.Description, request.SortOrder, request.IsActive, request.ShowInMenu, parentId);
            await repository.AddAsync(category, cancellationToken);
            logger.LogInformation("Created category {CategoryId}.", category.Id);
            return response.Success(ToDetails(category), "Category created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception)
        {
            logger.LogError(exception, "Category creation validation failed for slug {Slug}.", request.Slug);
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private static CategoryDetailsResponse ToDetails(Category category)
    {
        return new CategoryDetailsResponse
        {
            Id = category.Id, ParentId = category.ParentId, Name = category.Name, Slug = category.Slug,
            Description = category.Description, SortOrder = category.SortOrder, IsActive = category.IsActive,
            ShowInMenu = category.ShowInMenu, CreatedAt = category.CreatedAt, UpdatedAt = category.UpdatedAt
        };
    }
}
