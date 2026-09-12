namespace FashionStore.API.Features.Auth.AdminForgotPassword;
public sealed class AdminForgotPasswordService(UserManager<ApplicationUser> userManager) : IAdminForgotPasswordService
{
    public async Task<ResponseResult> ExecuteAsync(string actorId, string userId, AdminForgotPasswordRequest request)
    {
        var response = new ResponseResult();
        var actor = await userManager.FindByIdAsync(actorId);
        var user = await userManager.FindByIdAsync(userId);
        if (actor is null || user is null) return response.Fail("User was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (!await userManager.CheckPasswordAsync(actor, request.AdminPassword)) return response.Fail("The administrator password is invalid.", ResponseCodes.ACTION_NOT_PERMITTED);
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal)) return response.Fail("The new passwords do not match.", ResponseCodes.INVALID_ACTION);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        return result.Succeeded ? response.Success("User password reset successfully.") : response.Fail("The user password could not be reset.", ResponseCodes.ACTION_FAILED, result.Errors.Select(error => error.Description).ToArray());
    }
}
