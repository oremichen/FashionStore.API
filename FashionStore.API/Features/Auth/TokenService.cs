using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FashionStore.Infrastructure.Contracts.Abstractions.Auth;
using FashionStore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace FashionStore.API.Features.Auth
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, DateTimeOffset expiresAtUtc)
        {
            _logger.LogInformation("Generating JWT for user {UserId} expiring at {ExpiresAtUtc}.", user.Id, expiresAtUtc);
            var secret = _configuration["JwtSettings:Secret"]
                ?? throw new InvalidOperationException("JWT secret is not configured.");
            var issuer = _configuration["JwtSettings:Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is not configured.");
            var audience = _configuration["JwtSettings:Audience"]
                ?? throw new InvalidOperationException("JWT audience is not configured.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                new(ClaimTypes.Surname, user.LastName ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAtUtc.UtcDateTime,
                signingCredentials: credentials);

            var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogInformation("Generated JWT for user {UserId}.", user.Id);
            return serializedToken;
        }

    }
}
