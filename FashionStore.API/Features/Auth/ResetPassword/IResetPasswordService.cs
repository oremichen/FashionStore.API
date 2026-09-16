namespace FashionStore.API.Features.Auth.ResetPassword;

public interface IResetPasswordService
{
    Task<ResponseResult> ExecuteAsync(string userId, ResetPasswordRequest request, CancellationToken cancellationToken);
}
