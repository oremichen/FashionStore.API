namespace FashionStore.Application.Abstractions.CatalogOptions;

public interface ICatalogOptionService
{
    Task<ResponseResult<IReadOnlyList<SizeResponse>>> GetSizesAsync(CancellationToken cancellationToken);
    Task<ResponseResult<SizeResponse>> CreateSizeAsync(CreateSizeRequest request, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteSizeAsync(string id, CancellationToken cancellationToken);
    Task<ResponseResult<IReadOnlyList<ColorResponse>>> GetColorsAsync(CancellationToken cancellationToken);
    Task<ResponseResult<ColorResponse>> CreateColorAsync(CreateColorRequest request, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteColorAsync(string id, CancellationToken cancellationToken);
}
