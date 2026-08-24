namespace FashionStore.API.Features.Brands.GetBrands;

public sealed class GetBrandsService(BrandOperations operations) : IGetBrandsService
{
    public Task<ResponseResult<IReadOnlyList<BrandResponse>>> ExecuteAsync(CancellationToken cancellationToken) => operations.GetAllAsync(cancellationToken);
}
