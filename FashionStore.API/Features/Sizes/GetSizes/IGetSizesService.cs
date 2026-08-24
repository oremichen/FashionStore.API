namespace FashionStore.API.Features.Sizes.GetSizes;

public interface IGetSizesService
{
    Task<ResponseResult<IReadOnlyList<SizeResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
