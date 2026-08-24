namespace FashionStore.API.Features.Colors.CreateColor;

public interface ICreateColorService
{
    Task<ResponseResult<ColorResponse>> ExecuteAsync(CreateColorRequest request, CancellationToken cancellationToken);
}
