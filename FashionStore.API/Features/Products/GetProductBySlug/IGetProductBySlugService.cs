namespace FashionStore.API.Features.Products.GetProductBySlug;

public interface IGetProductBySlugService
{
    Task<ResponseResult<ProductDetailResponse>> ExecuteAsync(string slug, CancellationToken cancellationToken);
}
