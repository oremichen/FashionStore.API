namespace FashionStore.API.Features.Products.GetProductCollection;

public sealed class GetProductCollectionService(ProductOperations operations) : IGetProductCollectionService
{
    public Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(string collection, int page, int pageSize, CancellationToken cancellationToken) =>
        operations.GetCollectionAsync(collection, page, pageSize, cancellationToken);
}
