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

        public async Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(string userId)
        {
            var response = new ResponseResult<UserDetailsResponse>();
            _logger.LogInformation("Get current user requested for {UserId}.", userId);
            var user = await _userManager.Users
                .Include(item => item.Addresses)
                .SingleOrDefaultAsync(item => item.Id == userId);
            if (user == null)
            {
                _logger.LogError("Get current user failed for {UserId}: user was not found.", userId);
                return response.Fail("No user was found for the current token.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
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
                Roles = roles.ToList(),
                Addresses = user.Addresses
                    .OrderByDescending(address => address.IsMain)
                    .ThenBy(address => address.Id)
                    .Select(UserAddressResponse.From)
                    .ToList()
            };
        }
    }
}
