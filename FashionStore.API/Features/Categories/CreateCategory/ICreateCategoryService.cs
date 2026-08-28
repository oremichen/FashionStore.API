namespace FashionStore.API.Features.Categories.CreateCategory;

public interface ICreateCategoryService
{
    Task<ResponseResult<CategoryDetailsResponse>> ExecuteAsync(CreateCategoryRequest request, CancellationToken cancellationToken);
}
