namespace FashionStore.API.Features.Products.GetRelatedProducts;

public interface IGetRelatedProductsService
{
    Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(string productId, int page, int pageSize, CancellationToken cancellationToken);
}
