namespace FashionStore.API.Features.Auth.Logout;

public interface ILogoutService
{
    Task<ResponseResult> ExecuteAsync(string username, string tokenId);
}
