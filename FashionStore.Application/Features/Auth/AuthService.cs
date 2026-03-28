using FashionStore.Application.Abstractions.Auth;
using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;
using FashionStore.Domain.Entities;
using FashionStore.Shared.Common;
using FashionStore.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FashionStore.Application.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ResponseResult<LoginResponse>> Login(LoginRequest login)
        {
            var response = new ResponseResult<LoginResponse>();

            _logger.LogInformation("Login attempt received for email {Email}.", login.Email);

            var user = await _userManager.FindByEmailAsync(login.Email);
            if (user == null)
            {
                _logger.LogError("Login failed for email {Email}: user was not found.", login.Email);
                return response.Fail("Invalid email or password.", ResponseCodes.INVALID_ACTION);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogError(
                    "Login blocked for user {UserId} with email {Email}: account is inactive. Deleted: {IsDeleted}, Deactivated: {IsDeactivated}.",
                    user.Id,
                    user.Email,
                    user.IsDeleted,
                    user.IsDeactivated);
                return response.Fail("This account is not active.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogError(
                    "Login blocked for user {UserId} with email {Email}: email not confirmed.",
                    user.Id,
                    user.Email);
                return response.Fail("Email address has not been confirmed.", ResponseCodes.ACTION_NOT_PERMITTED);
            }

            var passwordIsValid = await _userManager.CheckPasswordAsync(user, login.Password);
            if (!passwordIsValid)
            {
                _logger.LogError(
                    "Login failed for user {UserId} with email {Email}: invalid password.",
                    user.Id,
                    user.Email);
                return response.Fail("Invalid email or password.", ResponseCodes.INVALID_ACTION);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokenExpiry = DateTimeOffset.UtcNow.AddHours(1);
            var token = _tokenService.GenerateJwtToken(user, roles, tokenExpiry);

            user.LastLoginDate = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation(
                "Login successful for user {UserId} with email {Email}. Roles: {Roles}.",
                user.Id,
                user.Email,
                string.Join(", ", roles));

            return response.Success(new LoginResponse
            {
                AccessToken = token,
                ExpiresAtUtc = tokenExpiry,
                TokenType = "Bearer"
            }, "Login successful.");
        }

    }
}
