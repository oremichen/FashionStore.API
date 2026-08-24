using System.Security.Cryptography;

namespace FashionStore.API.Features.Users.UpdateUser
{
    public class UpdateUserService : IUpdateUserService
    {
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UpdateUserService> _logger;
        public UpdateUserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, IConfiguration configuration, ILogger<UpdateUserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(UpdateUserDetailsRequest request)
        {
            var response = new ResponseResult<UserDetailsResponse>();
            _logger.LogInformation("Update user details requested for current email {CurrentEmail}.", request.CurrentEmail);
            var user = await _userManager.FindByEmailAsync(request.CurrentEmail);
            if (user == null)
            {
                _logger.LogError("Update user details failed for {CurrentEmail}: user was not found.", request.CurrentEmail);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            var requestedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            if (!string.IsNullOrWhiteSpace(requestedEmail) && !string.Equals(user.Email, requestedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existingUserWithNewEmail = await _userManager.FindByEmailAsync(requestedEmail);
                if (existingUserWithNewEmail != null && existingUserWithNewEmail.Id != user.Id)
                {
                    _logger.LogError("Update user details rejected for user {UserId}: email {Email} already belongs to another account.", user.Id, requestedEmail);
                    return response.Fail("A user with this email already exists.", ResponseCodes.DUPLICATE_RECORD);
                }
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            if (!string.IsNullOrWhiteSpace(requestedEmail))
            {
                user.Email = requestedEmail;
                user.UserName = requestedEmail;
                user.NormalizedEmail = _userManager.NormalizeEmail(requestedEmail);
                user.NormalizedUserName = _userManager.NormalizeName(requestedEmail);
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogError("Update user details failed for user {UserId}. Errors: {Errors}.", user.Id, string.Join(" | ", errors));
                return response.Fail(string.Join(" ", errors), ResponseCodes.ACTION_FAILED, errors);
            }

            return response.Success(await BuildUserDetailsResponse(user), "User details updated successfully.");
        }

        private async Task<UserDetailsResponse> BuildUserDetailsResponse(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return new UserDetailsResponse
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };
        }
    }
}
