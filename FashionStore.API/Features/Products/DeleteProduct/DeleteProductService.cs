namespace FashionStore.API.Features.Products.DeleteProduct;

public sealed class DeleteProductService(ProductOperations operations) : IDeleteProductService
{
    public Task<ResponseResult> ExecuteAsync(string productId, CancellationToken cancellationToken) =>
        operations.DeleteAsync(productId, cancellationToken);
}
