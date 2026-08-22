namespace FashionStore.Application.Abstractions.Brands;

public interface IBrandService
{
    Task<ResponseResult<BrandResponse>> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken);
    Task<ResponseResult<IReadOnlyList<BrandResponse>>> GetAllAsync(CancellationToken cancellationToken);
    Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken);
}
