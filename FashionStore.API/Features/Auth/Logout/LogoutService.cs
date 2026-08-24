namespace FashionStore.API.Features.Auth.Logout;

public sealed class LogoutService(AuthOperations operations) : ILogoutService
{
    public Task<ResponseResult> ExecuteAsync(string username, string tokenId) => operations.Logout(username, tokenId);
}
