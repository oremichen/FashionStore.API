using System.Security.Cryptography;

namespace FashionStore.Application.Features.Users
{
    public class UserManagementService : IUserManagementService
    {
        private const int TemporaryPasswordLength = 12;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailNotificationService emailNotificationService,
            IEmailTemplateRenderer emailTemplateRenderer,
            IConfiguration configuration,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResponseResult<UserDetailsResponse>> GetUserByEmail(string email)
        {
            var response = new ResponseResult<UserDetailsResponse>();

            _logger.LogInformation("Get user by email requested for {Email}.", email);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogError("Get user by email failed for {Email}: user was not found.", email);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            return response.Success(await BuildUserDetailsResponse(user), "User retrieved successfully.");
        }

        public async Task<ResponseResult<UserDetailsResponse>> UpdateUserDetails(UpdateUserDetailsRequest request)
        {
            var response = new ResponseResult<UserDetailsResponse>();

            _logger.LogInformation("Update user details requested for current email {CurrentEmail}.", request.CurrentEmail);

            var user = await _userManager.FindByEmailAsync(request.CurrentEmail);
            if (user == null)
            {
                _logger.LogError("Update user details failed for {CurrentEmail}: user was not found.", request.CurrentEmail);
                return response.Fail("No user was found for the supplied email address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
            }

            var requestedEmail = string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim();

            if (!string.IsNullOrWhiteSpace(requestedEmail)
                && !string.Equals(user.Email, requestedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existingUserWithNewEmail = await _userManager.FindByEmailAsync(requestedEmail);
                if (existingUserWithNewEmail != null && existingUserWithNewEmail.Id != user.Id)
                {
                    _logger.LogError(
                        "Update user details rejected for user {UserId}: email {Email} already belongs to another account.",
                        user.Id,
                        requestedEmail);
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

                _logger.LogError(
                    "Update user details failed for user {UserId}. Errors: {Errors}.",
                    user.Id,
                    string.Join(" | ", errors));

                return response.Fail(
                    string.Join(" ", errors),
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            return response.Success(await BuildUserDetailsResponse(user), "User details updated successfully.");
        }

        public async Task<ResponseResult<UserDetailsResponse>> CreateUser(CreateUserRequest request)
        {
            var response = new ResponseResult<UserDetailsResponse>();

            _logger.LogInformation("Create user requested for email {Email}.", request.Email);

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogError("Create user rejected for email {Email}: user already exists.", request.Email);
                return response.Fail("A user with this email already exists.", ResponseCodes.DUPLICATE_RECORD);
            }

            var requestedRoles = request.Roles?
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];

            if (requestedRoles.Count == 0)
            {
                requestedRoles.Add(RoleEnums.User.ToString());
            }

            foreach (var role in requestedRoles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(role);
                if (!roleExists)
                {
                    _logger.LogError("Create user rejected for email {Email}: role {Role} was not found.", request.Email, role);
                    return response.Fail($"Role '{role}' does not exist.", ResponseCodes.INVALID_ACTION);
                }
            }

            var user = new ApplicationUser
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                EmailConfirmed = true,
                EmailVerified = true,
                UserStatus = "Active",
                IsDeactivated = false,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var temporaryPassword = GenerateTemporaryPassword();
            var createResult = await _userManager.CreateAsync(user, temporaryPassword);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(error => error.Description).ToArray();

                _logger.LogError(
                    "Create user failed for email {Email}. Errors: {Errors}.",
                    request.Email,
                    string.Join(" | ", errors));

                return response.Fail(
                    string.Join(" ", errors),
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            var roleResult = await _userManager.AddToRolesAsync(user, requestedRoles);
            if (!roleResult.Succeeded)
            {
                var errors = roleResult.Errors.Select(error => error.Description).ToArray();
                await _userManager.DeleteAsync(user);

                _logger.LogError(
                    "Create user role assignment failed for email {Email}. Errors: {Errors}. User creation was rolled back.",
                    request.Email,
                    string.Join(" | ", errors));

                return response.Fail(
                    "User created but failed to assign roles.",
                    ResponseCodes.ACTION_FAILED,
                    errors);
            }

            user.IsPasswordChanged = false;
            user.PasswordChangedAt = null;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            await SendUserCreationMail(user, temporaryPassword, requestedRoles);

            return response.Success(await BuildUserDetailsResponse(user), "User created successfully.");
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

        private async Task SendUserCreationMail(ApplicationUser user, string temporaryPassword, IEnumerable<string> roles)
        {
            var appName = GetAppName();
            var loginPageUrl = _configuration["Frontend:LoginPageUrl"]
                ?? throw new InvalidOperationException("No login page link");
            var assignedRoles = string.Join(", ", roles);

            var emailBody = await _emailTemplateRenderer.RenderAsync(
                EmailNotificationTypeEnum.UserCreation,
                new Dictionary<string, string>
                {
                    ["appName"] = appName,
                    ["username"] = $"{user.FirstName} {user.LastName}".Trim(),
                    ["temporaryPassword"] = temporaryPassword,
                    ["roles"] = assignedRoles,
                    ["loginUrl"] = loginPageUrl,
                    ["year"] = DateTime.UtcNow.Year.ToString()
                });

            await _emailNotificationService.QueueEmailAsync(new EmailNotification
            {
                To = new List<string> { user.Email! },
                Subject = $"{appName} account created",
                Body = emailBody
            });
        }

        private string GetAppName()
        {
            return _configuration["AppSettings:AppName"]
                ?? throw new InvalidOperationException("No application name configured");
        }

        private static string GenerateTemporaryPassword()
        {
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowercase = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*";
            const string all = uppercase + lowercase + digits + special;

            var passwordCharacters = new List<char>
            {
                GetRandomCharacter(uppercase),
                GetRandomCharacter(lowercase),
                GetRandomCharacter(digits),
                GetRandomCharacter(special)
            };

            while (passwordCharacters.Count < TemporaryPasswordLength)
            {
                passwordCharacters.Add(GetRandomCharacter(all));
            }

            ShuffleCharacters(passwordCharacters);
            return new string(passwordCharacters.ToArray());
        }

        private static char GetRandomCharacter(string characters)
        {
            var index = RandomNumberGenerator.GetInt32(characters.Length);
            return characters[index];
        }

        private static void ShuffleCharacters(IList<char> characters)
        {
            for (var index = characters.Count - 1; index > 0; index--)
            {
                var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
                (characters[index], characters[swapIndex]) = (characters[swapIndex], characters[index]);
            }
        }
    }
}
