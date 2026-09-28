using FashionStore.Domain.Abstractions.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FashionStore.API.Features.Auth.Refresh;

public sealed class RefreshService(
    IAuthSessionRepository authSessionRepository,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<RefreshService> logger) : IRefreshService
{
    private const string BearerSchemePrefix = "Bearer ";

    public async Task<ResponseResult<LoginResponse>> ExecuteAsync(RefreshRequest request)
    {
        var response = new ResponseResult<LoginResponse>();
        var now = DateTimeOffset.UtcNow;
        if (!TryValidateExpiredAccessToken(now, out var accessTokenClaims))
        {
            return response.Fail("A valid expired access token is required to refresh this session.", ResponseCodes.INVALID_TOKEN);
        }

        var hash = SessionPolicy.HashRefreshToken(request.RefreshToken);
        var session = await authSessionRepository.GetByRefreshTokenHashWithUserAsync(hash, CancellationToken.None);

        if (session is null || session.RevokedAtUtc is not null || session.AbsoluteExpiresAtUtc <= now ||
            session.IdleExpiresAtUtc <= now || session.SecurityStamp != (session.User.SecurityStamp ?? string.Empty) ||
            session.User.IsDeleted || session.User.IsDeactivated)
        {
            if (session is not null && session.RevokedAtUtc is null)
            {
                session.RevokedAtUtc = now;
                await authSessionRepository.SaveChangesAsync(CancellationToken.None);
            }
            return response.Fail("The session has expired. Please sign in again.", ResponseCodes.INVALID_TOKEN);
        }

        var accessTokenUserId = accessTokenClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        var accessTokenSessionId = accessTokenClaims.FindFirst("sid")?.Value;
        if (!string.Equals(accessTokenUserId, session.UserId, StringComparison.Ordinal) ||
            !string.Equals(accessTokenSessionId, session.Id, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Refresh request access token does not match session {SessionId} for user {UserId}.",
                session.Id,
                session.UserId);
            return response.Fail("The access token does not belong to this session.", ResponseCodes.INVALID_TOKEN);
        }

        var roles = await userManager.GetRolesAsync(session.User);
        var isAdmin = roles.Any(SessionPolicy.IsAdminRole);
        var rotatedRefreshToken = SessionPolicy.CreateRefreshToken();
        session.RefreshTokenHash = SessionPolicy.HashRefreshToken(rotatedRefreshToken);
        session.LastUsedAtUtc = now;
        session.LastIpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        session.IdleExpiresAtUtc = isAdmin
            ? now.Add(SessionPolicy.AdminIdleLifetime)
            : Min(now.Add(SessionPolicy.CustomerRollingLifetime), session.AbsoluteExpiresAtUtc);
        await authSessionRepository.SaveChangesAsync(CancellationToken.None);

        var accessExpiry = Min(
            now.Add(isAdmin ? SessionPolicy.AdminAccessLifetime : SessionPolicy.CustomerAccessLifetime),
            session.AbsoluteExpiresAtUtc);
        var accessToken = tokenService.GenerateJwtToken(session.User, roles, accessExpiry, session.Id);
        logger.LogInformation("Rotated refresh token for session {SessionId} and user {UserId}.", session.Id, session.UserId);

        return response.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = rotatedRefreshToken,
            ExpiresAtUtc = accessExpiry,
            UserFirstName = session.User.FirstName ?? string.Empty,
            ImageUrl = session.User.AvatarUrl,
            UserName = session.User.Email ?? string.Empty,
            UserRoles = roles.ToList(),
            IsAdminSession = isAdmin
        }, "Session refreshed.");
    }

    private bool TryValidateExpiredAccessToken(DateTimeOffset now, out ClaimsPrincipal claims)
    {
        claims = new ClaimsPrincipal();
        var authorizationHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(BearerSchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError("Refresh request did not include a bearer access token.");
            return false;
        }

        var accessToken = authorizationHeader[BearerSchemePrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogError("Refresh request included an empty bearer access token.");
            return false;
        }

        try
        {
            var secret = configuration["JwtSettings:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                logger.LogCritical("Refresh token validation cannot run because the JWT secret is not configured.");
                return false;
            }

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidAudience = configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };

            var handler = new JwtSecurityTokenHandler();
            claims = handler.ValidateToken(accessToken, tokenValidationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwt || jwt.ValidTo == DateTime.MinValue || jwt.ValidTo > now.UtcDateTime)
            {
                logger.LogError("Refresh request included an access token that has not expired.");
                claims = new ClaimsPrincipal();
                return false;
            }

            return true;
        }
        catch (SecurityTokenException exception)
        {
            logger.LogWarning(exception, "Refresh request included an invalid access token.");
            return false;
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "Refresh request included a malformed access token.");
            return false;
        }
    }

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right)
    {
        return left <= right ? left : right;
    }
}
