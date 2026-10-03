namespace FashionStore.API.Features.Products.GetStorefront;

public interface IGetStorefrontService
{
    Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(StorefrontProductQuery query, string? userId, CancellationToken cancellationToken);
}
