namespace FashionStore.API.Features.Categories.UpdateCategory;

public interface IUpdateCategoryService
{
    Task<ResponseResult<CategoryDetailsResponse>> ExecuteAsync(string id, UpdateCategoryRequest request, CancellationToken cancellationToken);
}
