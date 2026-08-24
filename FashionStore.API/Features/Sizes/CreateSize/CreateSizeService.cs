namespace FashionStore.API.Features.Sizes.CreateSize;

public sealed class CreateSizeService(FashionStore.API.Features.CatalogOptions.CatalogOptionOperations operations) : ICreateSizeService
{
    public Task<ResponseResult<SizeResponse>> ExecuteAsync(CreateSizeRequest request, CancellationToken cancellationToken) => operations.CreateSizeAsync(request, cancellationToken);
}
