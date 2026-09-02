using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.API.Features.Users.UpdateUser;

public sealed class UpdateUserService(
    UserManager<ApplicationUser> userManager,
    FashionStoreDbContext dbContext,
    IEmailNotificationService emailNotificationService,
    IEmailTemplateRenderer emailTemplateRenderer,
    IConfiguration configuration,
    ILogger<UpdateUserService> logger) : IUpdateUserService
{
    public async Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(string userId, UpdateUserDetailsRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<UserDetailsResponse>();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return response.Fail("No user was found for the current token.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var oldEmail = user.Email ?? string.Empty;
        var requestedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var emailChanged = requestedEmail is not null && !string.Equals(oldEmail, requestedEmail, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || !await userManager.CheckPasswordAsync(user, request.CurrentPassword))
                return response.Fail("Your current password is required to change your email address.", ResponseCodes.ACTION_NOT_PERMITTED);
            var existingUser = await userManager.FindByEmailAsync(requestedEmail!);
            if (existingUser is not null && existingUser.Id != user.Id)
                return response.Fail("A user with this email already exists.", ResponseCodes.DUPLICATE_RECORD);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        if (emailChanged)
        {
            user.Email = requestedEmail;
            user.UserName = requestedEmail;
            user.NormalizedEmail = userManager.NormalizeEmail(requestedEmail!);
            user.NormalizedUserName = userManager.NormalizeName(requestedEmail!);
            user.EmailConfirmed = false;
            user.EmailVerified = false;
            user.UserStatus = "PendingConfirmation";
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return response.Fail(string.Join(" ", updateResult.Errors.Select(error => error.Description)), ResponseCodes.ACTION_FAILED);

        if (emailChanged)
        {
            await userManager.UpdateSecurityStampAsync(user);
            var now = DateTimeOffset.UtcNow;
            await dbContext.UserSessions.Where(session => session.UserId == user.Id && session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAtUtc, now), cancellationToken);
            await SendEmailChangeNotificationsAsync(user, oldEmail);
        }

        var roles = await userManager.GetRolesAsync(user);
        return response.Success(new UserDetailsResponse
        {
            UserId = user.Id, FirstName = user.FirstName, LastName = user.LastName,
            Email = user.Email ?? string.Empty, Roles = roles.ToList()
        }, emailChanged ? "Email updated. Verify the new address before signing in again." : "User details updated successfully.");
    }

    private async Task SendEmailChangeNotificationsAsync(ApplicationUser user, string oldEmail)
    {
        var appName = configuration["AppSettings:AppName"] ?? "Fashion Store";
        var confirmationBaseUrl = configuration["Frontend:ConfirmationPageUrl"] ?? throw new InvalidOperationException("No confirmation page link");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationUrl = QueryHelpers.AddQueryString(confirmationBaseUrl,
            new Dictionary<string, string?> { ["email"] = user.Email, ["token"] = encodedToken });
        var body = await emailTemplateRenderer.RenderAsync(EmailNotificationTypeEnum.Registration,
            new Dictionary<string, string> { ["appName"] = appName, ["username"] = $"{user.FirstName} {user.LastName}".Trim(),
                ["confirmUrl"] = confirmationUrl, ["year"] = DateTime.UtcNow.Year.ToString() });
        await emailNotificationService.QueueEmailAsync(new EmailNotification
        {
            To = [user.Email!], Subject = $"Verify your new {appName} email address", Body = body
        });
        if (!string.IsNullOrWhiteSpace(oldEmail))
            await emailNotificationService.QueueEmailAsync(new EmailNotification
            {
                To = [oldEmail], Subject = $"Your {appName} email address was changed",
                Body = $"<p>The email address on your {appName} account was changed to {System.Net.WebUtility.HtmlEncode(user.Email)}.</p><p>If you did not make this change, contact support immediately.</p>"
            });
        logger.LogInformation("Email changed and all sessions revoked for user {UserId}.", user.Id);
    }
}
