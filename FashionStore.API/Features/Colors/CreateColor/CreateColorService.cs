namespace FashionStore.API.Features.Colors.CreateColor;

public sealed class CreateColorService(FashionStore.API.Features.CatalogOptions.CatalogOptionOperations operations) : ICreateColorService
{
    public Task<ResponseResult<ColorResponse>> ExecuteAsync(CreateColorRequest request, CancellationToken cancellationToken) => operations.CreateColorAsync(request, cancellationToken);
}
