namespace FashionStore.API.Features.Products.GetRelatedProducts;

public sealed class GetRelatedProductsService(ProductOperations operations) : IGetRelatedProductsService
{
    public Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(string productId, int page, int pageSize, CancellationToken cancellationToken) =>
        operations.GetRelatedAsync(productId, page, pageSize, cancellationToken);
}
