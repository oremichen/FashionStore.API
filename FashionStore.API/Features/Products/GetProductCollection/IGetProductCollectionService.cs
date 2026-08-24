namespace FashionStore.API.Features.Products.GetProductCollection;

public interface IGetProductCollectionService
{
    Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(string collection, int page, int pageSize, CancellationToken cancellationToken);
}
