namespace FashionStore.API.Features.Users.UpdateAdminUser;

public sealed class UpdateAdminUserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager) : IUpdateAdminUserService
{
    public async Task<ResponseResult> ExecuteAsync(string userId, UpdateAdminUserRequest request)
    {
        var response = new ResponseResult();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return response.Fail("User was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var selectedRoles = new List<string>();
        foreach (var roleId in request.RoleIds.Distinct())
        {
            var role = await roleManager.FindByIdAsync(roleId);
            if (role?.Name is null || !RoleConstants.AdminRoles.Contains(role.Name))
                return response.Fail("Only administrative roles can be assigned here.", ResponseCodes.INVALID_ACTION);
            selectedRoles.Add(role.Name);
        }
        var duplicate = await userManager.FindByEmailAsync(request.Email.Trim());
        if (duplicate is not null && duplicate.Id != user.Id)
            return response.Fail("A user with this email already exists.", ResponseCodes.DUPLICATE_RECORD);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim();
        user.UserName = user.Email;
        user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
        user.NormalizedUserName = userManager.NormalizeName(user.Email);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded) return response.Fail("Administrator could not be updated.", ResponseCodes.ACTION_FAILED, update.Errors.Select(error => error.Description).ToArray());

        var currentRoles = await userManager.GetRolesAsync(user);
        var remove = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!remove.Succeeded) return response.Fail("Existing roles could not be updated.", ResponseCodes.ACTION_FAILED);
        var add = await userManager.AddToRolesAsync(user, selectedRoles);
        return add.Succeeded ? response.Success("Administrator updated successfully.")
            : response.Fail("Administrative roles could not be assigned.", ResponseCodes.ACTION_FAILED, add.Errors.Select(error => error.Description).ToArray());
    }
}
