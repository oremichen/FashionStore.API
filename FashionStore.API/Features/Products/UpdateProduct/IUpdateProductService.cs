namespace FashionStore.API.Features.Products.UpdateProduct;

public interface IUpdateProductService
{
    Task<ResponseResult<ProductResponse>> ExecuteAsync(UpdateProductRequest request, CancellationToken cancellationToken);
}
