namespace FashionStore.API.Features.Products.DeleteProduct;

public interface IDeleteProductService
{
    Task<ResponseResult> ExecuteAsync(string productId, CancellationToken cancellationToken);
}
