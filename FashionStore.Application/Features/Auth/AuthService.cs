using FashionStore.Application.Abstractions.Auth;
using FashionStore.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace FashionStore.Application.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationQueueService _emailNotificationQueueService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            IEmailNotificationQueueService emailNotificationQueueService,
            IEmailTemplateRenderer emailTemplateRenderer,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationQueueService = emailNotificationQueueService;
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
                _logger.LogWarning("Login failed for email {Email}: user was not found.", login.Email);
                return response.Fail("Invalid email or password.", ResponseCodes.INVALID_ACTION);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogWarning(
                    "Login blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.",
                    user.Id,
                    user.Email,
                    user.IsDeleted,
                    user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning(
                    "Login blocked for user {UserId} with email {Email}: email not confirmed.",
                    user.Id,
                    user.Email);
                return response.Fail("Email address has not been confirmed.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            var passwordIsValid = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!passwordIsValid)
            {
                _logger.LogWarning(
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
                TokenType = "Bearer"
            }, "Login successful.");
        }

        public async Task<ResponseResult> Register(RegisterRequest request)
        {
            var response = new ResponseResult();

            _logger.LogInformation("Registration attempt received for email {Email}.", request.Email);

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
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
                EmailVerified = true,
                UserStatus = "Active",
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

            var roleResult = await _userManager.AddToRoleAsync(user, RoleEnums.User.ToString());
            if (!roleResult.Succeeded)
            {
                var errors = roleResult.Errors.Select(error => error.Description).ToArray();
                var deleteResult = await _userManager.DeleteAsync(user);

                _logger.LogError(
                    "User {UserId} was created but assigning role failed. Errors: {Errors}. User cleanup succeeded: {CleanupSucceeded}.",
                    user.Id,
                    string.Join(" | ", errors),
                    deleteResult.Succeeded);

                if (!deleteResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to delete user {UserId} after role assignment failure. Cleanup errors: {CleanupErrors}.",
                        user.Id,
                        string.Join(" | ", deleteResult.Errors.Select(error => error.Description)));
                }

                return response.Fail(
                    "User created but failed to assign default role.",
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            var loginUrl = _configuration["Frontend:LoginUrl"] ?? "http://localhost:4200/login";
            var emailBody = await _emailTemplateRenderer.RenderAsync(
                EmailNotificationTypeEnum.Registration,
                new Dictionary<string, string>
                {
                    ["username"] = $"{user.FirstName} {user.LastName}".Trim(),
                    ["confirmUrl"] = loginUrl,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

            _emailNotificationQueueService.Enqueue(new EmailNotification
            {
                To = new List<string> { user.Email! },
                Subject = "Welcome to FashionStore",
                Body = emailBody
            });

            _logger.LogInformation(
                "Registration successful for user {UserId} with email {Email}. Welcome email queued.",
                user.Id,
                user.Email);

            return response.Success("Registration successful. A welcome email has been queued for delivery.");
        }
    }
}
