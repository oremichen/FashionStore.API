namespace FashionStore.API.Features.Colors.DeleteColor;

public interface IDeleteColorService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
