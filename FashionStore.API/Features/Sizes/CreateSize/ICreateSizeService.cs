namespace FashionStore.API.Features.Sizes.CreateSize;

public interface ICreateSizeService
{
    Task<ResponseResult<SizeResponse>> ExecuteAsync(CreateSizeRequest request, CancellationToken cancellationToken);
}
