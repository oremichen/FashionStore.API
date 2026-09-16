namespace FashionStore.API.Features.Brands.GetBrands;

public interface IGetBrandsService
{
    Task<ResponseResult<IReadOnlyList<BrandResponse>>> ExecuteAsync(bool availableOnly, CancellationToken cancellationToken);
}
