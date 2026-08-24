namespace FashionStore.API.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryService(CategoryOperations operations) : IUpdateCategoryService
{
    public Task<ResponseResult<CategoryDetailsResponse>> ExecuteAsync(string id, UpdateCategoryRequest request, CancellationToken cancellationToken) => operations.UpdateAsync(id, request, cancellationToken);
}
