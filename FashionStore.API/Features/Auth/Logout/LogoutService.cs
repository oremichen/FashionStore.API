using FashionStore.Domain.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace FashionStore.API.Features.Auth.Logout
{
    public class LogoutService : ILogoutService
    {
        private static readonly TimeSpan ConfirmationResendCooldown = TimeSpan.FromMinutes(1);
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly ILogger<LogoutService> _logger;
        private readonly IConfiguration _configuration;
        public LogoutService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, ILogger<LogoutService> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ResponseResult> ExecuteAsync(string username, string tokenId)
        {
            var response = new ResponseResult();
            _logger.LogInformation("Logout request received for username {Username}.", username);
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(tokenId))
            {
                _logger.LogError("Logout rejected because token claims were incomplete. Username: {Username}, TokenId: {TokenId}.", username, tokenId);
                return response.Fail("The current token is invalid.", ResponseCodes.INVALID_TOKEN);
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                _logger.LogError("Logout failed for username {Username}: user was not found.", username);
                return response.Fail("No user was found for the current token.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            await _userManager.SetAuthenticationTokenAsync(user, AuthTokenConstants.JwtLoginProvider, BuildRevokedTokenName(tokenId), DateTimeOffset.UtcNow.ToString("O"));
            _logger.LogInformation("Logout successful for user {UserId} with username {Username}. Token {TokenId} was revoked.", user.Id, username, tokenId);
            return response.Success("Logout successful.");
        }

        private static string BuildRevokedTokenName(string tokenId)
        {
            return $"{AuthTokenConstants.RevokedTokenPrefix}{tokenId}";
        }
    }
}
