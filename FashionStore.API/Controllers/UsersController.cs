namespace FashionStore.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseApiController
    {
        private readonly IUserManagementService _userManagementService;

        public UsersController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        [HttpGet("by-email")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Get user by email")]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            var response = await _userManagementService.GetUserByEmail(email);
            return ProcessResponse(response);
        }

        [HttpPut]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Update user details")]
        public async Task<IActionResult> Update([FromBody] UpdateUserDetailsRequest request)
        {
            var response = await _userManagementService.UpdateUserDetails(request);
            return ProcessResponse(response);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Create user")]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            var response = await _userManagementService.CreateUser(request);
            return ProcessResponse(response);
        }
    }
}
