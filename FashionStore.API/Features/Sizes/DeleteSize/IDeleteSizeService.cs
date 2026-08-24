namespace FashionStore.API.Features.Sizes.DeleteSize;

public interface IDeleteSizeService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
