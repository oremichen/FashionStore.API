namespace FashionStore.API.Features.Types.DeleteType;

public interface IDeleteTypeService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
