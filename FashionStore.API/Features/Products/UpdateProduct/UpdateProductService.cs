namespace FashionStore.API.Features.Products.UpdateProduct;

public sealed class UpdateProductService(ProductOperations operations) : IUpdateProductService
{
    public Task<ResponseResult<ProductResponse>> ExecuteAsync(UpdateProductRequest request, CancellationToken cancellationToken) =>
        operations.UpdateAsync(request, cancellationToken);
}
