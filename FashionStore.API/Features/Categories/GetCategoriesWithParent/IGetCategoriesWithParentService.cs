namespace FashionStore.API.Features.Categories.GetCategoriesWithParent;

public interface IGetCategoriesWithParentService
{
    Task<ResponseResult<IReadOnlyList<CategoryResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
