namespace FashionStore.API.Features.Auth.AdminForgotPassword;
public interface IAdminForgotPasswordService
{
    Task<ResponseResult> ExecuteAsync(string actorId, string userId, AdminForgotPasswordRequest request);
}
