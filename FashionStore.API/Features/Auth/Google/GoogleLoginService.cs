using FashionStore.Domain.Abstractions.Auth;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;

namespace FashionStore.API.Features.Auth.Google;

public sealed class GoogleLoginService : IGoogleLoginService
{
    private const string ProviderName = "Google";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleLoginService> _logger;

    public GoogleLoginService(UserManager<ApplicationUser> userManager, ITokenService tokenService, IAuthSessionRepository authSessionRepository, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<GoogleLoginService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _authSessionRepository = authSessionRepository;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ResponseResult<LoginResponse>> ExecuteAsync(GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<LoginResponse>();
        _logger.LogInformation("Google sign-in request received.");

        var clientId = _configuration["Google:ClientId"];
        var clientSecret = _configuration["Google:ClientSecret"];
        var redirectUri = _configuration["Google:AuthorizationCodeRedirectUri"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
        {
            _logger.LogCritical("Google sign-in is not configured. Client ID, client secret, and authorization-code redirect URI are required.");
            return response.Fail("Google sign-in is not available right now.", ResponseCodes.ACTION_FAILED);
        }

        try
        {
            _logger.LogInformation("Google sign-in authorization code received.");
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { "openid", "email", "profile" }
            });
            var tokenResponse = await flow.ExchangeCodeForTokenAsync("google-sign-in", request.AuthorizationCode, redirectUri, cancellationToken);
            if (string.IsNullOrWhiteSpace(tokenResponse.IdToken))
            {
                _logger.LogError("Google token exchange did not return an ID token.");
                return response.Fail("Google did not return a valid identity token.", ResponseCodes.INVALID_ACTION);
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(tokenResponse.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });
            if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email) || payload.EmailVerified != true)
            {
                _logger.LogError("Google token validation completed without a verified subject and email claim.");
                return response.Fail("Your Google account email must be verified before you can sign in.", ResponseCodes.INVALID_ACTION);
            }

            var user = await _userManager.FindByLoginAsync(ProviderName, payload.Subject);
            if (user is null)
            {
                var matchingEmailUser = await _userManager.FindByEmailAsync(payload.Email);
                if (matchingEmailUser is not null)
                {
                    _logger.LogError("Google sign-in was not linked because email {Email} belongs to existing user {UserId}.", payload.Email, matchingEmailUser.Id);
                    return response.Fail("An account with this email already exists. Sign in with your password first to link Google.", ResponseCodes.DUPLICATE_RECORD);
                }

                user = new ApplicationUser
                {
                    FirstName = payload.GivenName ?? string.Empty,
                    LastName = payload.FamilyName ?? string.Empty,
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = true,
                    EmailVerified = true,
                    AvatarUrl = payload.Picture,
                    UserStatus = "Active",
                    IsDeactivated = false,
                    IsDeleted = false,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(error => error.Description).ToArray();
                    _logger.LogError("Google sign-in account creation failed for email {Email}. Errors: {Errors}.", payload.Email, string.Join(" | ", errors));
                    return response.Fail("Your account could not be created. Please try again.", ResponseCodes.ACTION_FAILED, errors);
                }

                var roleResult = await _userManager.AddToRoleAsync(user, RoleEnums.User.ToString());
                if (!roleResult.Succeeded)
                {
                    var errors = roleResult.Errors.Select(error => error.Description).ToArray();
                    await _userManager.DeleteAsync(user);
                    _logger.LogError("Google sign-in role assignment failed for user {UserId}. Errors: {Errors}.", user.Id, string.Join(" | ", errors));
                    return response.Fail("Your account could not be created. Please try again.", ResponseCodes.ACTION_FAILED, errors);
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(ProviderName, payload.Subject, ProviderName));
                if (!addLoginResult.Succeeded)
                {
                    var errors = addLoginResult.Errors.Select(error => error.Description).ToArray();
                    await _userManager.DeleteAsync(user);
                    _logger.LogError("Google login link creation failed for user {UserId}. Errors: {Errors}.", user.Id, string.Join(" | ", errors));
                    return response.Fail("Your account could not be created. Please try again.", ResponseCodes.ACTION_FAILED, errors);
                }

                _logger.LogInformation("Created customer account {UserId} through Google sign-in.", user.Id);
            }

            if (user.IsDeleted || user.IsDeactivated)
            {
                _logger.LogError("Google sign-in blocked for inactive user {UserId}.", user.Id);
                return response.Fail("This account is not active.", ResponseCodes.REQUEST_IN_PROGRESS);
            }

            return await CreateSessionAsync(user, response, cancellationToken);
        }
        catch (InvalidJwtException exception)
        {
            _logger.LogError(exception, "Google sign-in token validation failed.");
            return response.Fail("Google sign-in could not be verified.", ResponseCodes.INVALID_TOKEN);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Google sign-in failed while exchanging an authorization code.");
            return response.Fail("Login could not be completed. Please try again.", ResponseCodes.ACTION_FAILED);
        }
    }

    private async Task<ResponseResult<LoginResponse>> CreateSessionAsync(ApplicationUser user, ResponseResult<LoginResponse> response, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var now = DateTimeOffset.UtcNow;
        var isAdmin = roles.Any(SessionPolicy.IsAdminRole);
        var expiresAtUtc = now.Add(isAdmin ? SessionPolicy.AdminAccessLifetime : SessionPolicy.CustomerAccessLifetime);
        var refreshToken = SessionPolicy.CreateRefreshToken();
        var session = new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = SessionPolicy.HashRefreshToken(refreshToken),
            CreatedAtUtc = now,
            LastUsedAtUtc = now,
            IdleExpiresAtUtc = isAdmin ? now.Add(SessionPolicy.AdminIdleLifetime) : now.Add(SessionPolicy.CustomerRollingLifetime),
            AbsoluteExpiresAtUtc = now.Add(isAdmin ? SessionPolicy.AdminAbsoluteLifetime : SessionPolicy.CustomerAbsoluteLifetime),
            SecurityStamp = user.SecurityStamp ?? string.Empty,
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        };
        await _authSessionRepository.AddSessionAsync(session, cancellationToken);
        user.LastLoginDate = now;
        await _userManager.UpdateAsync(user);
        var accessToken = _tokenService.GenerateJwtToken(user, roles, expiresAtUtc, session.Id);
        _logger.LogInformation("Google sign-in completed for user {UserId}.", user.Id);
        return response.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = expiresAtUtc,
            TokenType = "Bearer",
            UserFirstName = user.FirstName ?? string.Empty,
            UserName = user.Email ?? string.Empty,
            ImageUrl = user.AvatarUrl,
            UserRoles = roles.ToList(),
            IsAdminSession = isAdmin
        }, "Login successful.");
    }

}
