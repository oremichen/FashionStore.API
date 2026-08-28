namespace FashionStore.API.Features.Products.DeleteProductImage;

public interface IDeleteProductImageService
{
    Task<ResponseResult> ExecuteAsync(string productId, string imageId, CancellationToken cancellationToken);
}
