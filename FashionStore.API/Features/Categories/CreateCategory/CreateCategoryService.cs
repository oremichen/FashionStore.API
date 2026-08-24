namespace FashionStore.API.Features.Categories.CreateCategory;

public sealed class CreateCategoryService(CategoryOperations operations) : ICreateCategoryService
{
    public Task<ResponseResult<CategoryDetailsResponse>> ExecuteAsync(CreateCategoryRequest request, CancellationToken cancellationToken) => operations.CreateAsync(request, cancellationToken);
}
