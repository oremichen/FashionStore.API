namespace FashionStore.API.Features.Sizes.DeleteSize;

public sealed class DeleteSizeService(FashionStore.API.Features.CatalogOptions.CatalogOptionOperations operations) : IDeleteSizeService
{
    public Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.DeleteSizeAsync(id, cancellationToken);
}
