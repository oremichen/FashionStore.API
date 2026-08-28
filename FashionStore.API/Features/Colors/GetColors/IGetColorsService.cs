namespace FashionStore.API.Features.Colors.GetColors;

public interface IGetColorsService
{
    Task<ResponseResult<IReadOnlyList<ColorResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
