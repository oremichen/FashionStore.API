using System.Security.Cryptography;
using System.Text;

namespace FashionStore.API.Features.Auth;

public static class SessionPolicy
{
    public static readonly TimeSpan CustomerAccessLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan CustomerRollingLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan CustomerAbsoluteLifetime = TimeSpan.FromDays(90);
    public static readonly TimeSpan AdminAccessLifetime = TimeSpan.FromMinutes(7);
    public static readonly TimeSpan AdminIdleLifetime = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan AdminAbsoluteLifetime = TimeSpan.FromHours(3);

    public static bool IsAdminRole(string role) =>
        role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("BusinessAdmin", StringComparison.OrdinalIgnoreCase);

    public static string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    public static string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
