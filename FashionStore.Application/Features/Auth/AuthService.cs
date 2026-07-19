using FashionStore.Application.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.Application.Features.Auth
{
    public class AuthService : IAuthService
    {
        private static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromMinutes(1);
        private const int TemporaryPasswordLength = 12;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IEmailNotificationService emailNotificationService,
            IEmailTemplateRenderer emailTemplateRenderer,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ResponseResult<LoginResponse>> Login(LoginRequest login)
        {
            var response = new ResponseResult<LoginResponse>();

            _logger.LogInformation("Login attempt received for email {Email}.", login.Email);

            var user = await _userManager.FindByEmailAsync(login.Email);
            if (user == null)
            {
                _logger.LogError("Login failed for email {Email}: user was not found.", login.Email);
                return response.Fail("Invalid email or password.", ResponseCodes.INVALID_ACTION);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogError(
                    "Login blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.",
                    user.Id,
                    user.Email,
                    user.IsDeleted,
                    user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.REQUEST_IN_PROGRESS);
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogError("Login user {UserId} with email {Email}: email not confirmed.", user.Id, user.Email);
                await SendConfirmationMail(user);
                return response.Fail("Email address has not been confirmed. A confirmation link has been sent to your email", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            var passwordIsValid = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!passwordIsValid)
            {
                _logger.LogError(
                    "Login failed for user {UserId} with email {Email}: invalid password.",
                    user.Id,
                    user.Email);
                return response.Fail("Invalid email or password.", ResponseCodes.INVALID_ACTION);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokenExpiry = DateTimeOffset.UtcNow.AddHours(1);
            var token = _tokenService.GenerateJwtToken(user, roles, tokenExpiry);

            user.LastLoginDate = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation(
                "Login successful for user {UserId} with email {Email}. Roles: {Roles}. Token expires at {TokenExpiryUtc}.",
                user.Id,
                user.Email,
                string.Join(", ", roles),
                tokenExpiry);

            return response.Success(new LoginResponse
            {
                AccessToken = token,
                ExpiresAtUtc = tokenExpiry,
                TokenType = "Bearer",
                UserFirstName = user.FirstName ?? string.Empty,
                UserName = user.Email ?? string.Empty,
                UserRoles = roles.ToList()
            }, "Login successful.");
        }

        public async Task<ResponseResult> Logout(string username, string tokenId)
        {
            var response = new ResponseResult();

            _logger.LogInformation("Logout request received for username {Username}.", username);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(tokenId))
            {
                _logger.LogWarning("Logout rejected because token claims were incomplete. Username: {Username}, TokenId: {TokenId}.", username, tokenId);
                return response.Fail("The current token is invalid.", ResponseCodes.INVALID_TOKEN);
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("Logout failed for username {Username}: user was not found.", username);
                return response.Fail("No user was found for the current token.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            await _userManager.SetAuthenticationTokenAsync(
                user,
                AuthTokenConstants.JwtLoginProvider,
                BuildRevokedTokenName(tokenId),
                DateTimeOffset.UtcNow.ToString("O"));

            _logger.LogInformation("Logout successful for user {UserId} with username {Username}. Token {TokenId} was revoked.", user.Id, username, tokenId);
            return response.Success("Logout successful.");
        }

        public async Task<ResponseResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var response = new ResponseResult();

            _logger.LogInformation("Forgot password request received for email {Email}.", request.Email);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Forgot password failed for email {Email}: user was not found.", request.Email);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogWarning(
                    "Forgot password blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.",
                    user.Id,
                    user.Email,
                    user.IsDeleted,
                    user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            if (!user.EmailConfirmed)
            {
                await SendConfirmationMail(user);
                _logger.LogWarning("Forgot password blocked for user {UserId} with email {Email}: email not confirmed.", user.Id, user.Email);
                return response.Fail("Email address has not been confirmed. A confirmation link has been sent to your email.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            var temporaryPassword = GenerateTemporaryPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);

            if (!resetResult.Succeeded)
            {
                var errors = resetResult.Errors.Select(error => error.Description).ToArray();

                _logger.LogWarning(
                    "Forgot password reset failed for user {UserId} with email {Email}. Errors: {Errors}.",
                    user.Id,
                    user.Email,
                    string.Join(" | ", errors));

                return response.Fail(
                    "A temporary password could not be generated. Please try again.",
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            user.IsPasswordChanged = false;
            user.PasswordChangedAt = null;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();

                _logger.LogWarning(
                    "Forgot password succeeded for user {UserId}, but profile update failed. Errors: {Errors}.",
                    user.Id,
                    string.Join(" | ", errors));

                return response.Fail(
                    "Temporary password was generated, but the account could not be fully updated.",
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);

            await SendForgotPasswordMail(user, temporaryPassword);

            _logger.LogInformation("Temporary password generated successfully for user {UserId} with email {Email}.", user.Id, user.Email);
            return response.Success("A temporary password has been sent to your email.");
        }

        public async Task<ResponseResult> ResetPassword(string username, ResetPasswordRequest request)
        {
            var response = new ResponseResult();

            _logger.LogInformation("Reset password request received for username {Username}.", username);

            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Reset password rejected because the authenticated username claim was missing.");
                return response.Fail("The current token is invalid.", ResponseCodes.INVALID_TOKEN);
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("Reset password failed for username {Username}: user was not found.", username);
                return response.Fail("No user was found for the current token.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogWarning(
                    "Reset password blocked for user {UserId} with username {Username}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.",
                    user.Id,
                    username,
                    user.IsDeleted,
                    user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                var errors = changePasswordResult.Errors.Select(error => error.Description).ToArray();

                _logger.LogWarning(
                    "Reset password failed for user {UserId} with username {Username}. Errors: {Errors}.",
                    user.Id,
                    username,
                    string.Join(" | ", errors));

                return response.Fail(
                    string.Join(" ", errors),
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            user.IsPasswordChanged = true;
            user.PasswordChangedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();

                _logger.LogWarning(
                    "Password changed for user {UserId}, but profile update failed. Errors: {Errors}.",
                    user.Id,
                    string.Join(" | ", errors));

                return response.Fail(
                    "Password was updated, but the account could not be fully updated.",
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            _logger.LogInformation("Password reset successful for user {UserId} with username {Username}.", user.Id, username);
            return response.Success("Password updated successfully.");
        }

        public async Task<ResponseResult> Register(RegisterRequest request)
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

                _logger.LogWarning(
                    "Registration failed for email {Email}. Errors: {Errors}.",
                    request.Email,
                    string.Join(" | ", errors));

                return response.Fail(
                    string.Join(" ", errors),
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            try
            {
                var roleResult = await _userManager.AddToRoleAsync(user, RoleEnums.User.ToString());
                if (!roleResult.Succeeded)
                {
                    var errors = roleResult.Errors.Select(error => error.Description).ToArray();
                    await ReverseUserCreationAsync(
                        user,
                        "role assignment failure",
                        string.Join(" | ", errors));

                    return response.Fail(
                        "User created but failed to assign default role.",
                        ResponseCodes.ACTION_FAILED,
                        errors);
                }

                await SendConfirmationMail(user);

                _logger.LogInformation(
                    "Registration successful for user {UserId} with email {Email}. Confirmation email queued.",
                    user.Id,
                    user.Email);

                return response.Success("Registration successful. A confirmation email has been queued for delivery.");
            }
            catch (Exception exception)
            {
                await ReverseUserCreationAsync(
                    user,
                    "post-creation registration failure",
                    exception.Message);

                _logger.LogError(
                    exception,
                    "Registration failed after creating user {UserId} with email {Email}. User creation was reversed.",
                    user.Id,
                    user.Email);

                return response.Fail(
                    "Registration could not be completed. Please try again.",
                    ResponseCodes.ACTION_FAILED);
            }
        }

        public async Task<ResponseResult> ConfirmEmail(ConfirmEmailRequest request)
        {
            var response = new ResponseResult();

            _logger.LogInformation("Email confirmation attempt received for email {Email}.", request.Email);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Email confirmation failed for email {Email}: user was not found.", request.Email);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogWarning(
                    "Email confirmation blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.",
                    user.Id,
                    user.Email,
                    user.IsDeleted,
                    user.IsDeactivated);
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
                _logger.LogWarning("Email confirmation failed for user {UserId}: token was empty after normalization.", user.Id);
                return response.Fail("A valid confirmation token is required.", ResponseCodes.INVALID_ACTION);
            }

            var confirmResult = await _userManager.ConfirmEmailAsync(user, confirmationToken);
            if (!confirmResult.Succeeded)
            {
                var errors = confirmResult.Errors.Select(error => error.Description).ToArray();

                _logger.LogWarning(
                    "Email confirmation failed for user {UserId} with email {Email}. Errors: {Errors}.",
                    user.Id,
                    user.Email,
                    string.Join(" | ", errors));

                return response.Fail(
                    "The confirmation token is invalid or has expired.",
                    ResponseCodes.INVALID_ACTION,
                    errors);
            }

            user.EmailVerified = true;
            user.UserStatus = "Active";
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();

                _logger.LogError(
                    "Email confirmed for user {UserId}, but profile update failed. Errors: {Errors}.",
                    user.Id,
                    string.Join(" | ", errors));

                return response.Fail(
                    "Email was confirmed, but the account could not be fully updated.",
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            await SendConfirmationSuccessMail(user);

            _logger.LogInformation("Email confirmation successful for user {UserId} with email {Email}.", user.Id, user.Email);
            return response.Success("Email confirmed successfully.");
        }

        public async Task<ResponseResult> ResendConfirmationLink(ResendConfirmationLinkRequest request)
        {
            var response = new ResponseResult();

            _logger.LogInformation("Resend confirmation link request received for email {Email}.", request.Email);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Resend confirmation link failed for email {Email}: user was not found.", request.Email);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogWarning(
                    "Resend confirmation link blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.",
                    user.Id,
                    user.Email,
                    user.IsDeleted,
                    user.IsDeactivated);
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

                    _logger.LogError(
                        "Resend confirmation link rate-limited for user {UserId} with email {Email}. Retry after {WaitSeconds} seconds.",
                        user.Id,
                        user.Email,
                        waitSeconds);

                    return response.Fail(
                        $"A confirmation email was sent recently. Please wait {waitSeconds} seconds before trying again.",
                        ResponseCodes.LIMIT_EXCEEDED);
                }
            }

            await SendConfirmationMail(user);

            _logger.LogInformation("Confirmation email re-queued for user {UserId} with email {Email}.", user.Id, user.Email);
            return response.Success("A new confirmation link has been sent to your email.");
        }

        #region Helper Functions
        private async Task ReverseUserCreationAsync(ApplicationUser user, string reason, string details)
        {
            var deleteResult = await _userManager.DeleteAsync(user);

            _logger.LogError(
                "Reversing user creation for user {UserId}. Reason: {Reason}. Details: {Details}. Cleanup succeeded: {CleanupSucceeded}.",
                user.Id,
                reason,
                details,
                deleteResult.Succeeded);

            if (!deleteResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to delete user {UserId} while reversing registration. Cleanup errors: {CleanupErrors}.",
                        user.Id,
                        string.Join(" | ", deleteResult.Errors.Select(error => error.Description)));
            }
        }

        private async Task SendConfirmationMail(ApplicationUser user)
        {
            var appName = GetAppName();
            var confirmationBaseUrl = _configuration["Frontend:ConfirmationPageUrl"] 
                ?? throw new InvalidOperationException("No confirmation page link"); 
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));
            var confirmationUrl = QueryHelpers.AddQueryString(
                confirmationBaseUrl,
                new Dictionary<string, string?>
                {
                    ["email"] = user.Email,
                    ["token"] = encodedToken
                });

            var emailBody = await _emailTemplateRenderer.RenderAsync(
                EmailNotificationTypeEnum.Registration,
                new Dictionary<string, string>
                {
                    ["appName"] = appName,
                    ["username"] = $"{user.FirstName} {user.LastName}".Trim(),
                    ["confirmUrl"] = confirmationUrl,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

            await _emailNotificationService.QueueEmailAsync(new EmailNotification
            {
                To = new List<string> { user.Email! },
                Subject = $"Welcome to {appName}",
                Body = emailBody
            });

            user.InviteResendDateTime = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning(
                    "Confirmation email was queued for user {UserId}, but resend tracking could not be updated. Errors: {Errors}.",
                    user.Id,
                    string.Join(" | ", updateResult.Errors.Select(error => error.Description)));
            }
        }

        private async Task SendConfirmationSuccessMail(ApplicationUser user)
        {
            var appName = GetAppName();
            var loginPageUrl = _configuration["Frontend:LoginPageUrl"]
                ?? throw new InvalidOperationException("No login page link");

            var emailBody = await _emailTemplateRenderer.RenderAsync(
                EmailNotificationTypeEnum.Confirmation,
                new Dictionary<string, string>
                {
                    ["appName"] = appName,
                    ["username"] = $"{user.FirstName} {user.LastName}".Trim(),
                    ["loginUrl"] = loginPageUrl,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

            await _emailNotificationService.QueueEmailAsync(new EmailNotification
            {
                To = new List<string> { user.Email! },
                Subject = $"{appName} email confirmation successful",
                Body = emailBody
            });
        }

        private async Task SendForgotPasswordMail(ApplicationUser user, string temporaryPassword)
        {
            var appName = GetAppName();
            var loginPageUrl = _configuration["Frontend:LoginPageUrl"]
                ?? throw new InvalidOperationException("No login page link");

            var emailBody = await _emailTemplateRenderer.RenderAsync(
                EmailNotificationTypeEnum.ForgotPassword,
                new Dictionary<string, string>
                {
                    ["appName"] = appName,
                    ["username"] = $"{user.FirstName} {user.LastName}".Trim(),
                    ["temporaryPassword"] = temporaryPassword,
                    ["loginUrl"] = loginPageUrl,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

            await _emailNotificationService.QueueEmailAsync(new EmailNotification
            {
                To = new List<string> { user.Email! },
                Subject = $"{appName} temporary password",
                Body = emailBody
            });
        }

        private string GetAppName()
        {
            return _configuration["AppSettings:AppName"]
                ?? throw new InvalidOperationException("No application name configured");
        }

        private static string BuildRevokedTokenName(string tokenId)
        {
            return $"{AuthTokenConstants.RevokedTokenPrefix}{tokenId}";
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
            return new string(passwordCharacters.ToArray());
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

        #endregion
    }
}
