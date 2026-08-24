using FashionStore.API.Features.Users.CreateUser;
using FashionStore.API.Features.Users.GetUserByEmail;
using FashionStore.API.Features.Users.UpdateUser;

namespace FashionStore.API.Features.Users
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseApiController
    {
        private readonly IGetUserByEmailService _getUserByEmailService;
        private readonly IUpdateUserService _updateUserService;
        private readonly ICreateUserService _createUserService;

        public UsersController(IGetUserByEmailService getUserByEmailService, IUpdateUserService updateUserService, ICreateUserService createUserService)
        {
            _getUserByEmailService = getUserByEmailService;
            _updateUserService = updateUserService;
            _createUserService = createUserService;
        }

        [HttpGet("by-email")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Get user by email")]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            var response = await _getUserByEmailService.ExecuteAsync(email);
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
            var response = await _updateUserService.ExecuteAsync(request);
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
            var response = await _createUserService.ExecuteAsync(request);
            return ProcessResponse(response);
        }
    }
}
