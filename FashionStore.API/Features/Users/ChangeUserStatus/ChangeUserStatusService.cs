namespace FashionStore.API.Features.Users.ChangeUserStatus;

public sealed class ChangeUserStatusService(
    UserManager<ApplicationUser> userManager,
    IEmailNotificationService emailService,
    IEmailTemplateRenderer templateRenderer,
    IConfiguration configuration,
    ILogger<ChangeUserStatusService> logger) : IChangeUserStatusService
{
    public async Task<ResponseResult> ExecuteAsync(string userId, ChangeUserStatusRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return response.Fail("User was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var now = DateTimeOffset.UtcNow;
        switch (request.Status)
        {
            case AccountStatus.Active:
                user.IsDeactivated = false;
                user.IsDeleted = false;
                user.DeletedAt = null;
                user.UserStatus = "Active";
                break;
            case AccountStatus.Deactivated:
                user.IsDeactivated = true;
                user.IsDeleted = false;
                user.DeletedAt = null;
                user.UserStatus = "Deactivated";
                break;
            case AccountStatus.Deleted:
                user.IsDeactivated = true;
                user.IsDeleted = true;
                user.DeletedAt = now;
                user.UserStatus = "Deleted";
                break;
            default:
                return response.Fail("Invalid account status.", ResponseCodes.INVALID_ACTION);
        }

        user.UpdatedAt = now;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            var errors = update.Errors.Select(error => error.Description).ToArray();
            return response.Fail("User status could not be changed.", ResponseCodes.ACTION_FAILED, errors);
        }

        await SendNotificationsAsync(user, request.Status, cancellationToken);
        logger.LogInformation("User {UserId} status changed to {Status}.", user.Id, request.Status);
        return response.Success($"User status changed to {request.Status}.");
    }

    private async Task SendNotificationsAsync(ApplicationUser user, AccountStatus status, CancellationToken cancellationToken)
    {
        var appName = configuration["AppSettings:AppName"] ?? throw new InvalidOperationException("No application name configured");
        var statusText = status.ToString().ToLowerInvariant();
        var tokens = new Dictionary<string, string>
        {
            ["appName"] = appName,
            ["username"] = $"{user.FirstName} {user.LastName}".Trim(),
            ["email"] = user.Email ?? string.Empty,
            ["status"] = statusText,
            ["year"] = DateTime.UtcNow.Year.ToString()
        };
        var body = await templateRenderer.RenderAsync(EmailNotificationTypeEnum.UserStatusChanged, tokens);
        if (!string.IsNullOrWhiteSpace(user.Email))
            await emailService.QueueEmailAsync(new EmailNotification { To = [user.Email], Subject = $"{appName} account {statusText}", Body = body }, cancellationToken);

        var superAdmins = await userManager.GetUsersInRoleAsync(RoleConstants.SuperAdmin);
        var recipients = superAdmins.Select(admin => admin.Email).Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email!).Where(email => !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (recipients.Count > 0)
            await emailService.QueueEmailAsync(new EmailNotification { To = recipients, Subject = $"User account {statusText}: {user.Email}", Body = body }, cancellationToken);
    }
}
