namespace FashionStore.API.Features.Products.GetProductImages;

public sealed class GetProductImagesService(ProductOperations operations) : IGetProductImagesService
{
    public Task<ResponseResult<IReadOnlyList<ProductImageResponse>>> ExecuteAsync(string productId, CancellationToken cancellationToken) =>
        operations.GetImagesAsync(productId, cancellationToken);
}
