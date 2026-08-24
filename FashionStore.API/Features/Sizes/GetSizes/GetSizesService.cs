namespace FashionStore.API.Features.Sizes.GetSizes;

public sealed class GetSizesService(FashionStore.API.Features.CatalogOptions.CatalogOptionOperations operations) : IGetSizesService
{
    public Task<ResponseResult<IReadOnlyList<SizeResponse>>> ExecuteAsync(CancellationToken cancellationToken) => operations.GetSizesAsync(cancellationToken);
}
