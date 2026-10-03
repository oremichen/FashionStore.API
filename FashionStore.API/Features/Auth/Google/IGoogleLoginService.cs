namespace FashionStore.API.Features.Auth.Google;

public interface IGoogleLoginService
{
    Task<ResponseResult<LoginResponse>> ExecuteAsync(GoogleLoginRequest request, CancellationToken cancellationToken);
}
