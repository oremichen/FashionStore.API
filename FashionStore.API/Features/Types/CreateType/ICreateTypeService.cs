namespace FashionStore.API.Features.Types.CreateType;

public interface ICreateTypeService
{
    Task<ResponseResult<TypeResponse>> ExecuteAsync(CreateTypeRequest request, CancellationToken cancellationToken);
}
