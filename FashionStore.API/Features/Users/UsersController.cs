using FashionStore.API.Features.Users.CreateUser;
using FashionStore.API.Features.Users.GetUserByEmail;
using FashionStore.API.Features.Users.UpdateUser;
using FashionStore.API.Features.Users.CreateUserAddress;
using FashionStore.API.Features.Users.DeleteUserAddress;
using FashionStore.API.Features.Users.GetAllUserAddresses;
using FashionStore.API.Features.Users.UpdateUserAddress;

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
        private readonly IGetAllUserAddressesService _getAllUserAddressesService;
        private readonly ICreateUserAddressService _createUserAddressService;
        private readonly IUpdateUserAddressService _updateUserAddressService;
        private readonly IDeleteUserAddressService _deleteUserAddressService;

        public UsersController(IGetUserByEmailService getUserByEmailService, IUpdateUserService updateUserService,
            ICreateUserService createUserService, IGetAllUserAddressesService getAllUserAddressesService,
            ICreateUserAddressService createUserAddressService, IUpdateUserAddressService updateUserAddressService,
            IDeleteUserAddressService deleteUserAddressService)
        {
            _getUserByEmailService = getUserByEmailService;
            _updateUserService = updateUserService;
            _createUserService = createUserService;
            _getAllUserAddressesService = getAllUserAddressesService;
            _createUserAddressService = createUserAddressService;
            _updateUserAddressService = updateUserAddressService;
            _deleteUserAddressService = deleteUserAddressService;
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

        [HttpGet("{userId}/addresses")]
        public async Task<IActionResult> GetAllUserAddresses(string userId, CancellationToken cancellationToken)
        {
            return ProcessResponse(await _getAllUserAddressesService.ExecuteAsync(userId, cancellationToken));
        }

        [HttpPost("{userId}/addresses")]
        public async Task<IActionResult> CreateUserAddress(string userId, [FromBody] UserAddressRequest request, CancellationToken cancellationToken)
        {
            return ProcessResponse(await _createUserAddressService.ExecuteAsync(userId, request, cancellationToken));
        }

        [HttpPut("{userId}/addresses/{addressId}")]
        public async Task<IActionResult> UpdateUserAddress(string userId, string addressId, [FromBody] UserAddressRequest request, CancellationToken cancellationToken)
        {
            return ProcessResponse(await _updateUserAddressService.ExecuteAsync(userId, addressId, request, cancellationToken));
        }

        [HttpDelete("{userId}/addresses/{addressId}")]
        public async Task<IActionResult> DeleteUserAddress(string userId, string addressId, CancellationToken cancellationToken)
        {
            return ProcessResponse(await _deleteUserAddressService.ExecuteAsync(userId, addressId, cancellationToken));
        }
    }
}
