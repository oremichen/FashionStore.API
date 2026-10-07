using FashionStore.Domain.Abstractions.Auth;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FashionStore.API.Features.Auth.Logout
{
    public class LogoutService : ILogoutService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LogoutService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAuthSessionRepository _authSessionRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogoutService(UserManager<ApplicationUser> userManager, ILogger<LogoutService> logger, IConfiguration configuration, IAuthSessionRepository authSessionRepository, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _authSessionRepository = authSessionRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new ResponseResult();
            _logger.LogInformation("Logout request received.");

            if (!TryValidateAccessToken(out var claims))
            {
                return response.Fail("The current token is invalid.", ResponseCodes.INVALID_TOKEN);
            }

            var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = claims.FindFirst("sid")?.Value;
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
            {
                _logger.LogError("Logout rejected because the signed token did not include a user id or session id.");
                return response.Fail("The current token is invalid.", ResponseCodes.INVALID_TOKEN);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("Logout failed because user {UserId} was not found.", userId);
                return response.Fail("No user was found for the current token.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            var session = await _authSessionRepository.GetByIdAndUserIdAsync(sessionId, user.Id, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            if (session is null || session.RevokedAtUtc is not null || session.AbsoluteExpiresAtUtc <= now ||
                session.IdleExpiresAtUtc <= now || session.SecurityStamp != (user.SecurityStamp ?? string.Empty))
            {
                _logger.LogError("Logout rejected because session {SessionId} for user {UserId} is not active.", sessionId, user.Id);
                return response.Fail("The current token is invalid.", ResponseCodes.INVALID_TOKEN);
            }

            session.RevokedAtUtc = now;
            await _authSessionRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Logout successful for user {UserId}. Session {SessionId} was revoked.", user.Id, sessionId);
            return response.Success("Logout successful.");
        }

        private bool TryValidateAccessToken(out ClaimsPrincipal claims)
        {
            claims = new ClaimsPrincipal();
            var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            const string bearerSchemePrefix = "Bearer ";
            if (string.IsNullOrWhiteSpace(authorizationHeader) ||
                !authorizationHeader.StartsWith(bearerSchemePrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Logout request did not include a bearer access token.");
                return false;
            }

            var accessToken = authorizationHeader[bearerSchemePrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogError("Logout request included an empty bearer access token.");
                return false;
            }

            var secret = _configuration["JwtSettings:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogCritical("Logout token validation cannot run because the JWT secret is not configured.");
                return false;
            }

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    LifetimeValidator = ValidateTokenNotBefore
                };

                var handler = new JwtSecurityTokenHandler();
                claims = handler.ValidateToken(accessToken, tokenValidationParameters, out _);
                return true;
            }
            catch (SecurityTokenException exception)
            {
                _logger.LogError(exception, "Logout request included an invalid access token.");
                return false;
            }
            catch (ArgumentException exception)
            {
                _logger.LogError(exception, "Logout request included a malformed access token.");
                return false;
            }
        }

        private static bool ValidateTokenNotBefore(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters validationParameters)
        {
            return !notBefore.HasValue || notBefore.Value <= DateTime.UtcNow;
        }
    }
}
