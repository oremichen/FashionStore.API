namespace FashionStore.API.Features.Products.GetProducts;

public sealed class GetProductsService(ProductOperations operations) : IGetProductsService
{
    public Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(ProductQuery query, CancellationToken cancellationToken) =>
        operations.GetAsync(query, cancellationToken);
}
