namespace FashionStore.API.Features.Auth.Refresh;

public sealed class RefreshService(
    FashionStoreDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RefreshService> logger) : IRefreshService
{
    public async Task<ResponseResult<LoginResponse>> ExecuteAsync(RefreshRequest request)
    {
        var response = new ResponseResult<LoginResponse>();
        var now = DateTimeOffset.UtcNow;
        var hash = SessionPolicy.HashRefreshToken(request.RefreshToken);
        var session = await dbContext.UserSessions.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.RefreshTokenHash == hash);

        if (session is null || session.RevokedAtUtc is not null || session.AbsoluteExpiresAtUtc <= now ||
            session.IdleExpiresAtUtc <= now || session.SecurityStamp != (session.User.SecurityStamp ?? string.Empty) ||
            session.User.IsDeleted || session.User.IsDeactivated)
        {
            if (session is not null && session.RevokedAtUtc is null)
            {
                session.RevokedAtUtc = now;
                await dbContext.SaveChangesAsync();
            }
            return response.Fail("The session has expired. Please sign in again.", ResponseCodes.INVALID_TOKEN);
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
        await dbContext.SaveChangesAsync();

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
            UserName = session.User.Email ?? string.Empty,
            UserRoles = roles.ToList(),
            IsAdminSession = isAdmin
        }, "Session refreshed.");
    }

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) => left <= right ? left : right;
}
