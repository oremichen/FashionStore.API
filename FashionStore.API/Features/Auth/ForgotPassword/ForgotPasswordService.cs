using FashionStore.Domain.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.API.Features.Auth.ForgotPassword
{
    public class ForgotPasswordService : IForgotPasswordService
    {
        private static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromMinutes(1);
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<ForgotPasswordService> _logger;
        private readonly IConfiguration _configuration;
        public ForgotPasswordService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, ILogger<ForgotPasswordService> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ResponseResult> ExecuteAsync(ForgotPasswordRequest request)
        {
            var response = new ResponseResult();
            _logger.LogInformation("Forgot password request received for email {Email}.", request.Email);
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogError("Forgot password failed for email {Email}: user was not found.", request.Email);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogError("Forgot password blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.", user.Id, user.Email, user.IsDeleted, user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            if (!user.EmailConfirmed)
            {
                await SendConfirmationMail(user);
                _logger.LogError("Forgot password blocked for user {UserId} with email {Email}: email not confirmed.", user.Id, user.Email);
                return response.Fail("Email address has not been confirmed. A confirmation link has been sent to your email.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            var temporaryPassword = GenerateTemporaryPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);
            if (!resetResult.Succeeded)
            {
                var errors = resetResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Forgot password reset failed for user {UserId} with email {Email}. Errors: {Errors}.", user.Id, user.Email, string.Join(" | ", errors));
                return response.Fail("A temporary password could not be generated. Please try again.", ResponseCodes.ACTION_FAILED, errors);
            }

            user.IsPasswordChanged = false;
            user.PasswordChangedAt = null;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Forgot password succeeded for user {UserId}, but profile update failed. Errors: {Errors}.", user.Id, string.Join(" | ", errors));
                return response.Fail("Temporary password was generated, but the account could not be fully updated.", ResponseCodes.ACTION_FAILED, errors);
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);
            await SendForgotPasswordMail(user, temporaryPassword);
            _logger.LogInformation("Temporary password generated successfully for user {UserId} with email {Email}.", user.Id, user.Email);
            return response.Success("A temporary password has been sent to your email.");
        }

        private async Task SendConfirmationMail(ApplicationUser user)
        {
            var appName = GetAppName();
            var confirmationBaseUrl = _configuration["Frontend:ConfirmationPageUrl"] ?? throw new InvalidOperationException("No confirmation page link");
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));
            var confirmationUrl = QueryHelpers.AddQueryString(confirmationBaseUrl, new Dictionary<string, string?> { ["email"] = user.Email, ["token"] = encodedToken });
            var emailBody = await _emailTemplateRenderer.RenderAsync(EmailNotificationTypeEnum.Registration, new Dictionary<string, string> { ["appName"] = appName, ["username"] = $"{user.FirstName} {user.LastName}".Trim(), ["confirmUrl"] = confirmationUrl, ["year"] = DateTime.UtcNow.Year.ToString() });
            await _emailNotificationService.QueueEmailAsync(new EmailNotification { To = new List<string> { user.Email! }, Subject = $"Welcome to {appName}", Body = emailBody });
            user.InviteResendDateTime = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Confirmation email was queued for user {UserId}, but resend tracking could not be updated. Errors: {Errors}.", user.Id, string.Join(" | ", updateResult.Errors.Select(error => error.Description)));
            }
        }

        private async Task SendForgotPasswordMail(ApplicationUser user, string temporaryPassword)
        {
            var appName = GetAppName();
            var loginPageUrl = _configuration["Frontend:LoginPageUrl"] ?? throw new InvalidOperationException("No login page link");
            var emailBody = await _emailTemplateRenderer.RenderAsync(EmailNotificationTypeEnum.ForgotPassword, new Dictionary<string, string> { ["appName"] = appName, ["username"] = $"{user.FirstName} {user.LastName}".Trim(), ["temporaryPassword"] = temporaryPassword, ["loginUrl"] = loginPageUrl, ["year"] = DateTime.UtcNow.Year.ToString() });
            await _emailNotificationService.QueueEmailAsync(new EmailNotification { To = new List<string> { user.Email! }, Subject = $"{appName} temporary password", Body = emailBody });
        }

        private string GetAppName()
        {
            return _configuration["AppSettings:AppName"] ?? throw new InvalidOperationException("No application name configured");
        }

        private static string GenerateTemporaryPassword()
        {
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowercase = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*";
            const string all = uppercase + lowercase + digits + special;
            var passwordCharacters = new List<char>
            {
                GetRandomCharacter(uppercase),
                GetRandomCharacter(lowercase),
                GetRandomCharacter(digits),
                GetRandomCharacter(special)
            };
            while (passwordCharacters.Count < TemporaryPasswordLength)
            {
                passwordCharacters.Add(GetRandomCharacter(all));
            }

            ShuffleCharacters(passwordCharacters);
            return new string (passwordCharacters.ToArray());
        }

        private static char GetRandomCharacter(string characters)
        {
            var index = RandomNumberGenerator.GetInt32(characters.Length);
            return characters[index];
        }

        private static void ShuffleCharacters(IList<char> characters)
        {
            for (var index = characters.Count - 1; index > 0; index--)
            {
                var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
                (characters[index], characters[swapIndex]) = (characters[swapIndex], characters[index]);
            }
        }
    }
}
