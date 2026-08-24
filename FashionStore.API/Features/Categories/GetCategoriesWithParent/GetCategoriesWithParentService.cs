namespace FashionStore.API.Features.Categories.GetCategoriesWithParent;

public sealed class GetCategoriesWithParentService(CategoryOperations operations) : IGetCategoriesWithParentService
{
    public Task<ResponseResult<IReadOnlyList<CategoryResponse>>> ExecuteAsync(CancellationToken cancellationToken) => operations.GetCategoriesWithParentAsync(cancellationToken);
}
