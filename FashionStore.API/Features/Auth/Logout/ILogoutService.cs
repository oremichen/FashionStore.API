namespace FashionStore.API.Features.Auth.Logout;

public interface ILogoutService
{
    Task<ResponseResult> ExecuteAsync(CancellationToken cancellationToken);
}
