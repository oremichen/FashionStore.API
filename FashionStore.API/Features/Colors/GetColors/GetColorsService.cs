namespace FashionStore.API.Features.Colors.GetColors;

public sealed class GetColorsService(FashionStore.API.Features.CatalogOptions.CatalogOptionOperations operations) : IGetColorsService
{
    public Task<ResponseResult<IReadOnlyList<ColorResponse>>> ExecuteAsync(CancellationToken cancellationToken) => operations.GetColorsAsync(cancellationToken);
}
