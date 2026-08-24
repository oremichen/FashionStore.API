using System.Security.Cryptography;

namespace FashionStore.API.Features.Users.GetUserByEmail
{
    public class GetUserByEmailService : IGetUserByEmailService
    {
        private const int TemporaryPasswordLength = 12;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IEmailTemplateRenderer _emailTemplateRenderer;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GetUserByEmailService> _logger;
        public GetUserByEmailService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IEmailNotificationService emailNotificationService, IEmailTemplateRenderer emailTemplateRenderer, IConfiguration configuration, ILogger<GetUserByEmailService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailNotificationService = emailNotificationService;
            _emailTemplateRenderer = emailTemplateRenderer;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(string email)
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
