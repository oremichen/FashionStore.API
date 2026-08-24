namespace FashionStore.API.Features.Brands.DeleteBrand;

public interface IDeleteBrandService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
