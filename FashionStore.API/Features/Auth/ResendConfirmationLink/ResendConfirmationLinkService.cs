using FashionStore.Domain.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.API.Features.Auth.ResendConfirmationLink
{
    public class ResendConfirmationLinkService : IResendConfirmationLinkService
    {
        private static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromMinutes(1);
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<ResendConfirmationLinkService> _logger;
        private readonly IConfiguration _configuration;
        public ResendConfirmationLinkService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, ILogger<ResendConfirmationLinkService> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ResponseResult> ExecuteAsync(ResendConfirmationLinkRequest request)
        {
            var response = new ResponseResult();
            _logger.LogInformation("Resend confirmation link request received for email {Email}.", request.Email);
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogError("Resend confirmation link failed for email {Email}: user was not found.", request.Email);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogError("Resend confirmation link blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.", user.Id, user.Email, user.IsDeleted, user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Resend confirmation link skipped for user {UserId} with email {Email}: email already confirmed.", user.Id, user.Email);
                return response.Success("Email address is already confirmed. You can log in.");
            }

            var now = DateTimeOffset.UtcNow;
            if (user.InviteResendDateTime.HasValue)
            {
                var nextAllowedResendAt = user.InviteResendDateTime.Value.Add(ConfirmationResendCooldown);
                if (nextAllowedResendAt > now)
                {
                    var waitTime = nextAllowedResendAt - now;
                    var waitSeconds = Math.Max(1, (int)Math.Ceiling(waitTime.TotalSeconds));
                    _logger.LogError("Resend confirmation link rate-limited for user {UserId} with email {Email}. Retry after {WaitSeconds} seconds.", user.Id, user.Email, waitSeconds);
                    return response.Fail($"A confirmation email was sent recently. Please wait {waitSeconds} seconds before trying again.", ResponseCodes.LIMIT_EXCEEDED);
                }
            }

            await SendConfirmationMail(user);
            _logger.LogInformation("Confirmation email re-queued for user {UserId} with email {Email}.", user.Id, user.Email);
            return response.Success("A new confirmation link has been sent to your email.");
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

        private string GetAppName()
        {
            return _configuration["AppSettings:AppName"] ?? throw new InvalidOperationException("No application name configured");
        }
    }
}
