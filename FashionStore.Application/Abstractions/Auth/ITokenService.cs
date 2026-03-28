using FashionStore.Domain.Entities;

namespace FashionStore.Application.Abstractions.Auth
{
    public interface ITokenService
    {
        string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, DateTimeOffset expiresAtUtc);
    }
}
