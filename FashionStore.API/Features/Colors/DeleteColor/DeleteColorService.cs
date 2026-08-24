namespace FashionStore.API.Features.Colors.DeleteColor;

public sealed class DeleteColorService(FashionStore.API.Features.CatalogOptions.CatalogOptionOperations operations) : IDeleteColorService
{
    public Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.DeleteColorAsync(id, cancellationToken);
}
