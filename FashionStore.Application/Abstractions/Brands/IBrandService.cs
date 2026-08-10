namespace FashionStore.Application.Abstractions.Brands;

public interface IBrandService
{
    Task<ResponseResult<BrandResponse>> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken);
    Task<ResponseResult<IReadOnlyList<BrandResponse>>> GetAllAsync(CancellationToken cancellationToken);
    Task<BrandImageResponse?> GetImageAsync(string id, CancellationToken cancellationToken);
}
