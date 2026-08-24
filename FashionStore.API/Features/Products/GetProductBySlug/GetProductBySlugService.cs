namespace FashionStore.API.Features.Products.GetProductBySlug;

public sealed class GetProductBySlugService(ProductOperations operations) : IGetProductBySlugService
{
    public Task<ResponseResult<ProductDetailResponse>> ExecuteAsync(string slug, CancellationToken cancellationToken) =>
        operations.GetBySlugAsync(slug, cancellationToken);
}
