namespace FashionStore.API.Features.Auth.Login;

public sealed class LoginService(AuthOperations operations) : ILoginService
{
    public Task<ResponseResult<LoginResponse>> ExecuteAsync(LoginRequest request) => operations.Login(request);
}
