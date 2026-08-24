namespace FashionStore.API.Features.Products.CreateProduct;

public interface ICreateProductService
{
    Task<ResponseResult<ProductResponse>> ExecuteAsync(CreateProductRequest request, CancellationToken cancellationToken);
}
