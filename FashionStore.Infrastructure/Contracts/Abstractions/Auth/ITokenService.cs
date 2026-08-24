using FashionStore.Domain.Entities;

namespace FashionStore.Infrastructure.Contracts.Abstractions.Auth
{
    public interface ITokenService
    {
        string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, DateTimeOffset expiresAtUtc);
    }
}
