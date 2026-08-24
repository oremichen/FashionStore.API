namespace FashionStore.API.Features.Brands.DeleteBrand;

public sealed class DeleteBrandService(BrandOperations operations) : IDeleteBrandService
{
    public Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.DeleteAsync(id, cancellationToken);
}
