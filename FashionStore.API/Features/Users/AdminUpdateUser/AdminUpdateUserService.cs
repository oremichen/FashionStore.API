namespace FashionStore.API.Features.Users.AdminUpdateUser;
public sealed class AdminUpdateUserService(UserManager<ApplicationUser> userManager) : IAdminUpdateUserService
{
    public async Task<ResponseResult> ExecuteAsync(string actorId, string userId, AdminUpdateUserRequest request)
    {
        var response = new ResponseResult();
        var actor = await userManager.FindByIdAsync(actorId);
        var user = await userManager.FindByIdAsync(userId);
        if (actor is null || user is null) return response.Fail("User was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (!await userManager.CheckPasswordAsync(actor, request.AdminPassword)) return response.Fail("The administrator password is invalid.", ResponseCodes.ACTION_NOT_PERMITTED);
        var duplicate = await userManager.FindByEmailAsync(request.Email.Trim());
        if (duplicate is not null && duplicate.Id != user.Id) return response.Fail("A user with this email already exists.", ResponseCodes.DUPLICATE_RECORD);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim();
        user.UserName = user.Email;
        user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
        user.NormalizedUserName = userManager.NormalizeName(user.Email);
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? response.Success("User updated successfully.") : response.Fail("The user could not be updated.", ResponseCodes.ACTION_FAILED, result.Errors.Select(error => error.Description).ToArray());
    }
}
