namespace FashionStore.API.Features.Products.GetProductById;

public sealed class GetProductByIdService(ProductOperations operations) : IGetProductByIdService
{
    public Task<ResponseResult<ProductDetailResponse>> ExecuteAsync(string productId, CancellationToken cancellationToken) =>
        operations.GetByIdAsync(productId, cancellationToken);
}
