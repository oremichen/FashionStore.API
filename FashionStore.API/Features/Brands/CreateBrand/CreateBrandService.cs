namespace FashionStore.API.Features.Brands.CreateBrand;

public sealed class CreateBrandService(BrandOperations operations) : ICreateBrandService
{
    public Task<ResponseResult<BrandResponse>> ExecuteAsync(CreateBrandRequest request, CancellationToken cancellationToken) => operations.CreateAsync(request, cancellationToken);
}
