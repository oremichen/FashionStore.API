namespace FashionStore.API.Features.Brands.CreateBrand;

public interface ICreateBrandService
{
    Task<ResponseResult<BrandResponse>> ExecuteAsync(CreateBrandRequest request, CancellationToken cancellationToken);
}
