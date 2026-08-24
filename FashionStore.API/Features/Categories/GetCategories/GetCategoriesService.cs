namespace FashionStore.API.Features.Categories.GetCategories;

public sealed class GetCategoriesService(CategoryOperations operations) : IGetCategoriesService
{
    public Task<ResponseResult<IReadOnlyList<CategoryResponse>>> ExecuteAsync(CancellationToken cancellationToken) => operations.GetCategoriesAsync(cancellationToken);
}
