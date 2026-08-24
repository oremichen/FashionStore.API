namespace FashionStore.API.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordService(AuthOperations operations) : IForgotPasswordService
{
    public Task<ResponseResult> ExecuteAsync(ForgotPasswordRequest request) => operations.ForgotPassword(request);
}
