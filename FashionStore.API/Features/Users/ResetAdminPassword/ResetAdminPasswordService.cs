using System.Security.Cryptography;

namespace FashionStore.API.Features.Users.ResetAdminPassword;

public sealed class ResetAdminPasswordService(
    UserManager<ApplicationUser> userManager,
    IEmailNotificationService emailService,
    IEmailTemplateRenderer templateRenderer,
    IConfiguration configuration,
    ILogger<ResetAdminPasswordService> logger) : IResetAdminPasswordService
{
    private const int TemporaryPasswordLength = 12;

    public async Task<ResponseResult> ExecuteAsync(string userId, CancellationToken cancellationToken)
    {
        var response = new ResponseResult();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return response.Fail("User was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (user.IsDeleted || user.IsDeactivated)
            return response.Fail("The administrator account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any(RoleConstants.AdminRoles.Contains))
            return response.Fail("Passwords can only be reset for administrator accounts.", ResponseCodes.ACTION_NOT_PERMITTED);

        var temporaryPassword = GenerateTemporaryPassword();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, temporaryPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToArray();
            return response.Fail("The administrator password could not be reset.", ResponseCodes.ACTION_FAILED, errors);
        }

        user.IsPasswordChanged = false;
        user.PasswordChangedAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            var errors = update.Errors.Select(error => error.Description).ToArray();
            return response.Fail("The password was reset, but the account could not be fully updated.", ResponseCodes.ACTION_FAILED, errors);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
        await SendPasswordEmailAsync(user, temporaryPassword, cancellationToken);
        logger.LogInformation("Password reset by SuperAdmin for administrator {UserId}.", user.Id);
        return response.Success("A temporary password has been sent to the administrator's email.");
    }

    private async Task SendPasswordEmailAsync(ApplicationUser user, string temporaryPassword, CancellationToken cancellationToken)
    {
        var appName = configuration["AppSettings:AppName"] ?? throw new InvalidOperationException("No application name configured");
        var loginUrl = configuration["Frontend:LoginPageUrl"] ?? throw new InvalidOperationException("No login page link");
        var body = await templateRenderer.RenderAsync(EmailNotificationTypeEnum.ForgotPassword, new Dictionary<string, string>
        {
            ["appName"] = appName,
            ["username"] = $"{user.FirstName} {user.LastName}".Trim(),
            ["temporaryPassword"] = temporaryPassword,
            ["loginUrl"] = loginUrl,
            ["year"] = DateTime.UtcNow.Year.ToString()
        });
        await emailService.QueueEmailAsync(new EmailNotification { To = [user.Email!], Subject = $"{appName} administrator password reset", Body = body }, cancellationToken);
    }

    private static string GenerateTemporaryPassword()
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = uppercase + lowercase + digits + special;
        var characters = new List<char>
        {
            RandomCharacter(uppercase), RandomCharacter(lowercase), RandomCharacter(digits), RandomCharacter(special)
        };
        while (characters.Count < TemporaryPasswordLength) characters.Add(RandomCharacter(all));
        for (var index = characters.Count - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (characters[index], characters[swapIndex]) = (characters[swapIndex], characters[index]);
        }
        return new string(characters.ToArray());
    }

    private static char RandomCharacter(string characters) => characters[RandomNumberGenerator.GetInt32(characters.Length)];
}
