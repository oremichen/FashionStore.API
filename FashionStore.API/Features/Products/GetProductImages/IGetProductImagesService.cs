namespace FashionStore.API.Features.Products.GetProductImages;

public interface IGetProductImagesService
{
    Task<ResponseResult<IReadOnlyList<ProductImageResponse>>> ExecuteAsync(string productId, CancellationToken cancellationToken);
}
