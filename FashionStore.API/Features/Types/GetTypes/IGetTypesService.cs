namespace FashionStore.API.Features.Types.GetTypes;

public interface IGetTypesService
{
    Task<ResponseResult<PagedResponse<TypeResponse>>> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken);
}
