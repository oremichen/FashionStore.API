namespace FashionStore.API.Features.Auth.ResetPassword;

public sealed class ResetPasswordService(AuthOperations operations) : IResetPasswordService
{
    public Task<ResponseResult> ExecuteAsync(string username, ResetPasswordRequest request) => operations.ResetPassword(username, request);
}
