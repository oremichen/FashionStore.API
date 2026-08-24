namespace FashionStore.API.Features.Categories.GetCategoryById;

public interface IGetCategoryByIdService
{
    Task<ResponseResult<CategoryDetailsResponse>> ExecuteAsync(string id, CancellationToken cancellationToken);
}
