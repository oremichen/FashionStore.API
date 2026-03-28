using FashionStore.Application.Abstractions.Auth;
using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;
using FashionStore.Domain.Entities;
using FashionStore.Shared.Common;
using FashionStore.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace FashionStore.Application.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<ResponseResult<LoginResponse>> Login(LoginRequest login)
        {
            var response = new ResponseResult<LoginResponse>();

            var user = await _userManager.FindByEmailAsync(login.Email);
            if (user == null)
            {
                return response.Fail("Invalid email or password.", ResponseCodes.INVALID_ACTION);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            if (!user.EmailConfirmed)
            {
                return response.Fail("Email address has not been confirmed.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            var passwordIsValid = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!passwordIsValid)
            {
                return response.Fail("Invalid email or password.", ResponseCodes.INVALID_ACTION);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokenExpiry = DateTimeOffset.UtcNow.AddHours(1);
            var token = GenerateJwtToken(user, roles, tokenExpiry);

            user.LastLoginDate = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            return response.Success(new LoginResponse
            {
                AccessToken = token,
                ExpiresAtUtc = tokenExpiry,
                TokenType = "Bearer"
            }, "Login successful.");
        }

    }
}
