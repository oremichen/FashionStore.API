using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Auth;

public interface IAuthSessionRepository
{
    Task<UserSession?> GetByRefreshTokenHashWithUserAsync(string refreshTokenHash, CancellationToken cancellationToken);

    Task<UserSession?> GetByIdAndUserIdAsync(string sessionId, string userId, CancellationToken cancellationToken);

    Task AddSessionAsync(UserSession session, CancellationToken cancellationToken);

    Task RevokeAllSessionsForUserAsync(string userId, DateTimeOffset revokedAt, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
