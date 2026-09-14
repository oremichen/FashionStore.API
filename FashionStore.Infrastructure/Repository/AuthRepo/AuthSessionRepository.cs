using FashionStore.Domain.Abstractions.Auth;
using FashionStore.Domain.Entities;
using FashionStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.AuthRepo;

public sealed class AuthSessionRepository(FashionStoreDbContext dbContext) : IAuthSessionRepository
{
    public Task<UserSession?> GetByRefreshTokenHashWithUserAsync(string refreshTokenHash, CancellationToken cancellationToken)
    {
        return dbContext.UserSessions
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.RefreshTokenHash == refreshTokenHash, cancellationToken);
    }

    public Task<UserSession?> GetByIdAndUserIdAsync(string sessionId, string userId, CancellationToken cancellationToken)
    {
        return dbContext.UserSessions
            .SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
    }

    public async Task AddSessionAsync(UserSession session, CancellationToken cancellationToken)
    {
        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RevokeAllSessionsForUserAsync(string userId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        return dbContext.UserSessions
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAtUtc, revokedAt), cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
