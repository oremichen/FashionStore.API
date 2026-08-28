namespace FashionStore.API.Features.Auth.ResetPassword;

public interface IResetPasswordService
{
    Task<ResponseResult> ExecuteAsync(string username, ResetPasswordRequest request);
}
