using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Auth
{
    public interface ITokenService
    {
        string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, DateTimeOffset expiresAtUtc);
    }
}
