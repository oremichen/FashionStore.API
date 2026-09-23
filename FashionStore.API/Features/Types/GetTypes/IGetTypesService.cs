namespace FashionStore.API.Features.Types.GetTypes;

public interface IGetTypesService
{
    Task<ResponseResult<IReadOnlyList<TypeResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
