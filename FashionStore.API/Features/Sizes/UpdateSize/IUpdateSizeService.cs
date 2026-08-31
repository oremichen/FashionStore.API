namespace FashionStore.API.Features.Sizes.UpdateSize;

public interface IUpdateSizeService
{
    Task<ResponseResult<SizeResponse>> ExecuteAsync(string id, UpdateSizeRequest request, CancellationToken cancellationToken);
}
