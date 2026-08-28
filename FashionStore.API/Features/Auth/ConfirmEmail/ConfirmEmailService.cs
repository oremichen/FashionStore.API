using FashionStore.Domain.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.API.Features.Auth.ConfirmEmail
{
    public class ConfirmEmailService : IConfirmEmailService
    {
        private static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromMinutes(1);
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<ConfirmEmailService> _logger;
        private readonly IConfiguration _configuration;
        public ConfirmEmailService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, ILogger<ConfirmEmailService> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ResponseResult> ExecuteAsync(ConfirmEmailRequest request)
        {
            var response = new ResponseResult();
            _logger.LogInformation("Email confirmation attempt received for email {Email}.", request.Email);
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogError("Email confirmation failed for email {Email}: user was not found.", request.Email);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogError("Email confirmation blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.", user.Id, user.Email, user.IsDeleted, user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email already confirmed for user {UserId} with email {Email}.", user.Id, user.Email);
                return response.Success("Email address has already been confirmed.");
            }

            var confirmationToken = DecodeConfirmationToken(request.Token);
            if (string.IsNullOrWhiteSpace(confirmationToken))
            {
                _logger.LogError("Email confirmation failed for user {UserId}: token was empty after normalization.", user.Id);
                return response.Fail("A valid confirmation token is required.", ResponseCodes.INVALID_ACTION);
            }

            var confirmResult = await _userManager.ConfirmEmailAsync(user, confirmationToken);
            if (!confirmResult.Succeeded)
            {
                var errors = confirmResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Email confirmation failed for user {UserId} with email {Email}. Errors: {Errors}.", user.Id, user.Email, string.Join(" | ", errors));
                return response.Fail("The confirmation token is invalid or has expired.", ResponseCodes.INVALID_ACTION, errors);
            }

            user.EmailVerified = true;
            user.UserStatus = "Active";
            user.UpdatedAt = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Email confirmed for user {UserId}, but profile update failed. Errors: {Errors}.", user.Id, string.Join(" | ", errors));
                return response.Fail("Email was confirmed, but the account could not be fully updated.", ResponseCodes.ACTION_FAILED, errors);
            }

            await SendConfirmationSuccessMail(user);
            _logger.LogInformation("Email confirmation successful for user {UserId} with email {Email}.", user.Id, user.Email);
            return response.Success("Email confirmed successfully.");
        }

        private async Task SendConfirmationSuccessMail(ApplicationUser user)
        {
            var appName = GetAppName();
            var loginPageUrl = _configuration["Frontend:LoginPageUrl"] ?? throw new InvalidOperationException("No login page link");
            var emailBody = await _emailTemplateRenderer.RenderAsync(EmailNotificationTypeEnum.Confirmation, new Dictionary<string, string> { ["appName"] = appName, ["username"] = $"{user.FirstName} {user.LastName}".Trim(), ["loginUrl"] = loginPageUrl, ["year"] = DateTime.UtcNow.Year.ToString() });
            await _emailNotificationService.QueueEmailAsync(new EmailNotification { To = new List<string> { user.Email! }, Subject = $"{appName} email confirmation successful", Body = emailBody });
        }

        private string GetAppName()
        {
            return _configuration["AppSettings:AppName"] ?? throw new InvalidOperationException("No application name configured");
        }

        private static string DecodeConfirmationToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            var normalizedToken = Uri.UnescapeDataString(token.Trim());
            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return string.Empty;
            }

            normalizedToken = normalizedToken.Replace(" ", "+");
            // Raw ASP.NET Identity tokens commonly contain '+' '/' '=' after URL decoding.
            // When those are present, the token is already in the format ConfirmEmailAsync expects.
            if (normalizedToken.Contains('+') || normalizedToken.Contains('/') || normalizedToken.Contains('='))
            {
                return normalizedToken;
            }

            try
            {
                return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(normalizedToken));
            }
            catch (FormatException)
            {
                // Support older links where the raw identity token was placed directly in the query string.
                return normalizedToken.Replace(" ", "+");
            }
        }
    }
}
