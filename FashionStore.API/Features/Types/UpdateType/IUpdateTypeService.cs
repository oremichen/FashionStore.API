namespace FashionStore.API.Features.Types.UpdateType;

public interface IUpdateTypeService
{
    Task<ResponseResult<TypeResponse>> ExecuteAsync(string id, UpdateTypeRequest request, CancellationToken cancellationToken);
}
