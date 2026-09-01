using FashionStore.Domain.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.API.Features.Auth.Login
{
    public class LoginService : ILoginService
    {
        private static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromMinutes(1);
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<LoginService> _logger;
        private readonly IConfiguration _configuration;
        private readonly FashionStoreDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public LoginService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, ILogger<LoginService> logger, IConfiguration configuration, FashionStoreDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseResult<LoginResponse>> ExecuteAsync(LoginRequest login)
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
                _logger.LogError("Login blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.", user.Id, user.Email, user.IsDeleted, user.IsDeactivated);
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
                _logger.LogError("Login failed for user {UserId} with email {Email}: invalid password.", user.Id, user.Email);
                return response.Fail("Invalid email or password.", ResponseCodes.INVALID_ACTION);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var now = DateTimeOffset.UtcNow;
            var isAdmin = roles.Any(SessionPolicy.IsAdminRole);
            var tokenExpiry = now.Add(isAdmin ? SessionPolicy.AdminAccessLifetime : SessionPolicy.CustomerAccessLifetime);
            var refreshToken = SessionPolicy.CreateRefreshToken();
            var session = new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = SessionPolicy.HashRefreshToken(refreshToken),
                CreatedAtUtc = now,
                LastUsedAtUtc = now,
                IdleExpiresAtUtc = isAdmin ? now.Add(SessionPolicy.AdminIdleLifetime) : now.Add(SessionPolicy.CustomerRollingLifetime),
                AbsoluteExpiresAtUtc = now.Add(isAdmin ? SessionPolicy.AdminAbsoluteLifetime : SessionPolicy.CustomerAbsoluteLifetime),
                SecurityStamp = user.SecurityStamp ?? string.Empty,
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
                IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            };
            _dbContext.UserSessions.Add(session);
            await _dbContext.SaveChangesAsync();
            var token = _tokenService.GenerateJwtToken(user, roles, tokenExpiry, session.Id);
            user.LastLoginDate = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Login successful for user {UserId} with email {Email}. Roles: {Roles}. Token expires at {TokenExpiryUtc}.", user.Id, user.Email, string.Join(", ", roles), tokenExpiry);
            return response.Success(new LoginResponse { AccessToken = token, RefreshToken = refreshToken, ExpiresAtUtc = tokenExpiry, TokenType = "Bearer", UserFirstName = user.FirstName ?? string.Empty, UserName = user.Email ?? string.Empty, UserRoles = roles.ToList(), IsAdminSession = isAdmin }, "Login successful.");
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
