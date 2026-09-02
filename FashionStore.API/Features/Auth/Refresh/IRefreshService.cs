namespace FashionStore.API.Features.Auth.Refresh;

public interface IRefreshService
{
    Task<ResponseResult<LoginResponse>> ExecuteAsync(RefreshRequest request);
}
