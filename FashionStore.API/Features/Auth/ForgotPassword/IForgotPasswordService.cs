namespace FashionStore.API.Features.Auth.ForgotPassword;

public interface IForgotPasswordService
{
    Task<ResponseResult> ExecuteAsync(ForgotPasswordRequest request);
}
