namespace FashionStore.API.Features.Categories.GetCategoryById;

public sealed class GetCategoryByIdService(CategoryOperations operations) : IGetCategoryByIdService
{
    public Task<ResponseResult<CategoryDetailsResponse>> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.GetByIdAsync(id, cancellationToken);
}
