namespace FashionStore.API.Features.Products.DeleteProductImage;

public sealed class DeleteProductImageService(ProductOperations operations) : IDeleteProductImageService
{
    public Task<ResponseResult> ExecuteAsync(string productId, string imageId, CancellationToken cancellationToken) =>
        operations.DeleteImageAsync(productId, imageId, cancellationToken);
}
