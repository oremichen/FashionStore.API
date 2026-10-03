namespace FashionStore.API.Features.Categories.GetAllCategories;

public interface IGetAllCategoriesService
{
    Task<ResponseResult<IReadOnlyList<CategoryResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
