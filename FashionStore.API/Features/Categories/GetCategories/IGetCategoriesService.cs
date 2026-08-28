namespace FashionStore.API.Features.Categories.GetCategories;

public interface IGetCategoriesService
{
    Task<ResponseResult<IReadOnlyList<CategoryResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
