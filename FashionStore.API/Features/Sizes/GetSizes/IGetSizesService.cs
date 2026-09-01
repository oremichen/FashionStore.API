namespace FashionStore.API.Features.Sizes.GetSizes;

public interface IGetSizesService
{
    Task<ResponseResult<PagedResponse<SizeResponse>>> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken);
}
