namespace FashionStore.API.Features.Products.CreateProduct;

public sealed class CreateProductService(ProductOperations operations) : ICreateProductService
{
    public Task<ResponseResult<ProductResponse>> ExecuteAsync(CreateProductRequest request, CancellationToken cancellationToken) =>
        operations.CreateAsync(request, cancellationToken);
}
