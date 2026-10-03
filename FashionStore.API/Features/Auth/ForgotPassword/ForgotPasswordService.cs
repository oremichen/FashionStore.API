using FashionStore.Domain.Abstractions.Auth;
using System.Security.Cryptography;

namespace FashionStore.API.Features.Auth.ForgotPassword
{
    public class ForgotPasswordService : IForgotPasswordService
    {
        private const int TemporaryPasswordLength = 12;
        private const string ForgotPasswordResponseMessage = "If an eligible account exists for this email, password reset instructions will be sent shortly.";
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<ForgotPasswordService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAuthSessionRepository _authSessionRepository;
        public ForgotPasswordService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, ILogger<ForgotPasswordService> logger, IConfiguration configuration, IAuthSessionRepository authSessionRepository)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
            _authSessionRepository = authSessionRepository;
        }

        public async Task<ResponseResult> ExecuteAsync(ForgotPasswordRequest request)
        {
            var response = new ResponseResult();
            _logger.LogInformation("Forgot password request received for email {Email}.", request.Email);
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation("Forgot password request ignored because no account exists for email {Email}.", request.Email);
                return response.Success(ForgotPasswordResponseMessage);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogInformation("Forgot password request ignored for inactive user {UserId}. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.", user.Id, user.IsDeleted, user.IsDeactivated);
                return response.Success(ForgotPasswordResponseMessage);
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogInformation("Forgot password request ignored for user {UserId} because email is not confirmed.", user.Id);
                return response.Success(ForgotPasswordResponseMessage);
            }

            if (!await _userManager.HasPasswordAsync(user))
            {
                _logger.LogInformation("Forgot password request ignored for passwordless user {UserId}.", user.Id);
                return response.Success(ForgotPasswordResponseMessage);
            }

            var temporaryPassword = GenerateTemporaryPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);
            if (!resetResult.Succeeded)
            {
                var errors = resetResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Forgot password reset failed for user {UserId} with email {Email}. Errors: {Errors}.", user.Id, user.Email, string.Join(" | ", errors));
                return response.Success(ForgotPasswordResponseMessage);
            }

            user.IsPasswordChanged = false;
            user.PasswordChangedAt = null;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Forgot password succeeded for user {UserId}, but profile update failed. Errors: {Errors}.", user.Id, string.Join(" | ", errors));
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);
            var now = DateTimeOffset.UtcNow;
            await _authSessionRepository.RevokeAllSessionsForUserAsync(user.Id, now, CancellationToken.None);
            await SendForgotPasswordMail(user, temporaryPassword);
            _logger.LogInformation("Temporary password generated successfully for user {UserId} with email {Email}.", user.Id, user.Email);
            return response.Success(ForgotPasswordResponseMessage);
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
