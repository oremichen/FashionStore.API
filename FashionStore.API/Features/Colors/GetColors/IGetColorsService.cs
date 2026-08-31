namespace FashionStore.API.Features.Colors.GetColors;

public interface IGetColorsService
{
    Task<ResponseResult<PagedResponse<ColorResponse>>> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken);
}
