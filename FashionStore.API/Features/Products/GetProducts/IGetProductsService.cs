namespace FashionStore.API.Features.Products.GetProducts;

public interface IGetProductsService
{
    Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(ProductQuery query, CancellationToken cancellationToken);
}
