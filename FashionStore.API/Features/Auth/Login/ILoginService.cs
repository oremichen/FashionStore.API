namespace FashionStore.API.Features.Auth.Login;

public interface ILoginService
{
    Task<ResponseResult<LoginResponse>> ExecuteAsync(LoginRequest request);
}
