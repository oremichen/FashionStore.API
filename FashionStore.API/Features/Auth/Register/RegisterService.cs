using FashionStore.Domain.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.API.Features.Auth.Register
{
    public class RegisterService : IRegisterService
    {
        private static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromMinutes(1);
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<RegisterService> _logger;
        private readonly IConfiguration _configuration;
        public RegisterService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, ILogger<RegisterService> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ResponseResult> ExecuteAsync(RegisterRequest request)
        {
            var response = new ResponseResult();
            _logger.LogInformation("Registration attempt received for email {Email}.", request.Email);
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                // TODO: Hanlde SSO scenarios for google and facebook where user may already exist,
                // return message to user to login with SSO instead of registering again
                _logger.LogError("Registration rejected for email {Email}: user already exists.", request.Email);
                return response.Fail("A user with this email already exists.", ResponseCodes.DUPLICATE_RECORD);
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = false,
                EmailVerified = false,
                UserStatus = "PendingConfirmation",
                IsDeactivated = false,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Registration failed for email {Email}. Errors: {Errors}.", request.Email, string.Join(" | ", errors));
                return response.Fail(string.Join(" ", errors), ResponseCodes.ACTION_FAILED, errors);
            }

            try
            {
                var roleResult = await _userManager.AddToRoleAsync(user, RoleEnums.User.ToString());
                if (!roleResult.Succeeded)
                {
                    var errors = roleResult.Errors.Select(error => error.Description).ToArray();
                    await ReverseUserCreationAsync(user, "role assignment failure", string.Join(" | ", errors));
                    return response.Fail("User created but failed to assign default role.", ResponseCodes.ACTION_FAILED, errors);
                }

                await SendConfirmationMail(user);
                _logger.LogInformation("Registration successful for user {UserId} with email {Email}. Confirmation email queued.", user.Id, user.Email);
                return response.Success("Registration successful. A confirmation email has been queued for delivery.");
            }
            catch (Exception exception)
            {
                await ReverseUserCreationAsync(user, "post-creation registration failure", exception.Message);
                _logger.LogError(exception, "Registration failed after creating user {UserId} with email {Email}. User creation was reversed.", user.Id, user.Email);
                return response.Fail("Registration could not be completed. Please try again.", ResponseCodes.ACTION_FAILED);
            }
        }

#region Helper Functions
        private async Task ReverseUserCreationAsync(ApplicationUser user, string reason, string details)
        {
            var deleteResult = await _userManager.DeleteAsync(user);
            _logger.LogError("Reversing user creation for user {UserId}. Reason: {Reason}. Details: {Details}. Cleanup succeeded: {CleanupSucceeded}.", user.Id, reason, details, deleteResult.Succeeded);
            if (!deleteResult.Succeeded)
            {
                _logger.LogError("Failed to delete user {UserId} while reversing registration. Cleanup errors: {CleanupErrors}.", user.Id, string.Join(" | ", deleteResult.Errors.Select(error => error.Description)));
            }
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
#endregion
    }
}
