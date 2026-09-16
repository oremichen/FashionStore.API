using Microsoft.Extensions.Logging;

namespace FashionStore.API.Features.Users.UpdateAdminUser;

public sealed class UpdateAdminUserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<UpdateAdminUserService> logger) : IUpdateAdminUserService
{
    public async Task<ResponseResult> ExecuteAsync(string actorId, string userId, UpdateAdminUserRequest request)
    {
        var response = new ResponseResult();
        logger.LogInformation("Admin update started by actor {ActorId} targeting user {TargetUserId} with {RoleCount} role(s).",
            actorId, userId, request.RoleIds?.Count ?? 0);

        if (string.Equals(actorId, userId, StringComparison.Ordinal))
        {
            logger.LogError("Actor {ActorId} attempted to update their own profile through UpdateAdminUser; blocked.", actorId);
            return response.Fail("You cannot update your own profile through this endpoint.", ResponseCodes.INVALID_ACTION);
        }

        logger.LogInformation("Fetching target user {TargetUserId} for admin update by actor {ActorId}.", userId, actorId);
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            logger.LogError("Target user {TargetUserId} was not found for admin update by actor {ActorId}.", userId, actorId);
            return response.Fail("User was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        logger.LogInformation("Resolving {RoleCount} role id(s) for target user {TargetUserId} (actor={ActorId}).",
            request.RoleIds?.Count ?? 0, userId, actorId);
        var selectedRoles = new List<string>();
        foreach (var roleId in request.RoleIds.Distinct())
        {
            var role = await roleManager.FindByIdAsync(roleId);
            if (role?.Name is null || !RoleConstants.AdminRoles.Contains(role.Name))
            {
                logger.LogError("Role id {RoleId} (name={RoleName}) is invalid or non-admin for target user {TargetUserId}, actor={ActorId}.",
                    roleId, role?.Name, userId, actorId);
                return response.Fail("Only administrative roles can be assigned here.", ResponseCodes.INVALID_ACTION);
            }
            selectedRoles.Add(role.Name);
        }

        logger.LogInformation("Checking email uniqueness for '{Email}' on target user {TargetUserId}, actor={ActorId}.",
            request.Email, userId, actorId);
        var duplicate = await userManager.FindByEmailAsync(request.Email.Trim());
        if (duplicate is not null && duplicate.Id != user.Id)
        {
            logger.LogError("Email '{Email}' is already owned by user {DuplicateUserId}; cannot assign to target {TargetUserId} (actor={ActorId}).",
                request.Email, duplicate.Id, userId, actorId);
            return response.Fail("A user with this email already exists.", ResponseCodes.DUPLICATE_RECORD);
        }

        logger.LogInformation("Updating profile fields for target user {TargetUserId}, actor={ActorId}.", userId, actorId);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim();
        user.UserName = user.Email;
        user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
        user.NormalizedUserName = userManager.NormalizeName(user.Email);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        logger.LogInformation("Persisting profile update for target user {TargetUserId}, actor={ActorId}.", userId, actorId);
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            logger.LogError("UserManager.UpdateAsync failed for target user {TargetUserId}, actor={ActorId}. Errors: {Errors}.",
                userId, actorId, string.Join(" | ", update.Errors.Select(error => error.Description)));
            return response.Fail("Administrator could not be updated.", ResponseCodes.ACTION_FAILED, update.Errors.Select(error => error.Description).ToArray());
        }

        logger.LogInformation("Updating roles for target user {TargetUserId}, actor={ActorId}: removing existing roles and assigning {NewRoles}.",
            userId, actorId, string.Join(", ", selectedRoles));
        var currentRoles = await userManager.GetRolesAsync(user);
        var remove = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!remove.Succeeded)
        {
            logger.LogError("RemoveFromRolesAsync failed for target user {TargetUserId}, actor={ActorId}. Current roles being removed: {CurrentRoles}.",
                userId, actorId, string.Join(", ", currentRoles));
            return response.Fail("Existing roles could not be updated.", ResponseCodes.ACTION_FAILED);
        }

        var add = await userManager.AddToRolesAsync(user, selectedRoles);
        if (add.Succeeded)
        {
            logger.LogInformation("Admin update succeeded for target user {TargetUserId} by actor {ActorId}. New roles: {NewRoles}.",
                userId, actorId, string.Join(", ", selectedRoles));
            return response.Success("Administrator updated successfully.");
        }

        logger.LogError("AddToRolesAsync failed for target user {TargetUserId}, actor={ActorId}. Roles attempted: {Roles}. Errors: {Errors}.",
            userId, actorId, string.Join(", ", selectedRoles), string.Join(" | ", add.Errors.Select(error => error.Description)));
        return response.Fail("Administrative roles could not be assigned.", ResponseCodes.ACTION_FAILED, add.Errors.Select(error => error.Description).ToArray());
    }
}
