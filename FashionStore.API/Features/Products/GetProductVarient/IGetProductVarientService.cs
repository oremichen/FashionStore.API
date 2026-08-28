namespace FashionStore.API.Features.Products.GetProductVarient;

public interface IGetProductVarientService
{
    Task<ResponseResult<IReadOnlyList<ProductVariantResponse>>> ExecuteAsync(string productId, CancellationToken cancellationToken);
}
