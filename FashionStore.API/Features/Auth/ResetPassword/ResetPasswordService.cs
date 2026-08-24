using FashionStore.Domain.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.API.Features.Auth.ResetPassword
{
    public class ResetPasswordService : IResetPasswordService
    {
        private static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromMinutes(1);
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<ResetPasswordService> _logger;
        private readonly IConfiguration _configuration;
        public ResetPasswordService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, ILogger<ResetPasswordService> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ResponseResult> ExecuteAsync(string username, ResetPasswordRequest request)
        {
            var response = new ResponseResult();
            _logger.LogInformation("Reset password request received for username {Username}.", username);
            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogError("Reset password rejected because the authenticated username claim was missing.");
                return response.Fail("The current token is invalid.", ResponseCodes.INVALID_TOKEN);
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                _logger.LogError("Reset password failed for username {Username}: user was not found.", username);
                return response.Fail("No user was found for the current token.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogError("Reset password blocked for user {UserId} with username {Username}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.", user.Id, username, user.IsDeleted, user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                var errors = changePasswordResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Reset password failed for user {UserId} with username {Username}. Errors: {Errors}.", user.Id, username, string.Join(" | ", errors));
                return response.Fail(string.Join(" ", errors), ResponseCodes.ACTION_FAILED, errors);
            }

            user.IsPasswordChanged = true;
            user.PasswordChangedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Password changed for user {UserId}, but profile update failed. Errors: {Errors}.", user.Id, string.Join(" | ", errors));
                return response.Fail("Password was updated, but the account could not be fully updated.", ResponseCodes.ACTION_FAILED, errors);
            }

            _logger.LogInformation("Password reset successful for user {UserId} with username {Username}.", user.Id, username);
            return response.Success("Password updated successfully.");
        }
    }
}
