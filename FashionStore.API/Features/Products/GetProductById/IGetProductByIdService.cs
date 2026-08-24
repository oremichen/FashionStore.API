namespace FashionStore.API.Features.Products.GetProductById;

public interface IGetProductByIdService
{
    Task<ResponseResult<ProductDetailResponse>> ExecuteAsync(string productId, CancellationToken cancellationToken);
}
