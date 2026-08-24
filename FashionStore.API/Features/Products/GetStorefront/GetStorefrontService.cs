namespace FashionStore.API.Features.Products.GetStorefront;

public sealed class GetStorefrontService(ProductOperations operations) : IGetStorefrontService
{
    public Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(StorefrontProductQuery query, CancellationToken cancellationToken) =>
        operations.GetStorefrontAsync(query, cancellationToken);
}
