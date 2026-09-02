using FashionStore.API.Features.Users.CreateUser;
using FashionStore.API.Features.Users.GetUserByEmail;
using FashionStore.API.Features.Users.UpdateUser;
using FashionStore.API.Features.Users.CreateUserAddress;
using FashionStore.API.Features.Users.DeleteUserAddress;
using FashionStore.API.Features.Users.GetAllUserAddresses;
using FashionStore.API.Features.Users.UpdateUserAddress;
using FashionStore.API.Features.Users.GetUsers;
using FashionStore.API.Features.Users.ChangeUserStatus;
using FashionStore.API.Features.Users.ResetAdminPassword;
using FashionStore.API.Features.Users.GetAdminRoles;
using FashionStore.API.Features.Users.UpdateAdminUser;

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
        private readonly IGetUsersService _getUsersService;
        private readonly IChangeUserStatusService _changeUserStatusService;
        private readonly IResetAdminPasswordService _resetAdminPasswordService;
        private readonly IGetAdminRolesService _getAdminRolesService;
        private readonly IUpdateAdminUserService _updateAdminUserService;

        public UsersController(IGetUserByEmailService getUserByEmailService, IUpdateUserService updateUserService,
            ICreateUserService createUserService, IGetAllUserAddressesService getAllUserAddressesService,
            ICreateUserAddressService createUserAddressService, IUpdateUserAddressService updateUserAddressService,
            IDeleteUserAddressService deleteUserAddressService, IGetUsersService getUsersService,
            IChangeUserStatusService changeUserStatusService, IResetAdminPasswordService resetAdminPasswordService,
            IGetAdminRolesService getAdminRolesService, IUpdateAdminUserService updateAdminUserService)
        {
            _getUserByEmailService = getUserByEmailService;
            _updateUserService = updateUserService;
            _createUserService = createUserService;
            _getAllUserAddressesService = getAllUserAddressesService;
            _createUserAddressService = createUserAddressService;
            _updateUserAddressService = updateUserAddressService;
            _deleteUserAddressService = deleteUserAddressService;
            _getUsersService = getUsersService;
            _changeUserStatusService = changeUserStatusService;
            _resetAdminPasswordService = resetAdminPasswordService;
            _getAdminRolesService = getAdminRolesService;
            _updateAdminUserService = updateAdminUserService;
        }

        [Authorize(Roles = RoleConstants.SuperAdmin)]
        [HttpGet]
        [ProducesResponseType(typeof(ResponseResult<PagedResponse<GetUsersResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [EndpointSummary("Get paginated customers or administrators")]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        {
            return ProcessResponse(await _getUsersService.ExecuteAsync(query, cancellationToken));
        }

        [Authorize(Roles = RoleConstants.SuperAdmin)]
        [HttpPut("{userId}/status")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [EndpointSummary("Activate, deactivate, or soft-delete a user")]
        public async Task<IActionResult> ChangeStatus(string userId, [FromBody] ChangeUserStatusRequest request, CancellationToken cancellationToken)
        {
            return ProcessResponse(await _changeUserStatusService.ExecuteAsync(userId, request, cancellationToken));
        }

        [Authorize(Roles = RoleConstants.SuperAdmin)]
        [HttpPost("{userId}/reset-admin-password")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [EndpointSummary("Reset an administrator password and email a temporary password")]
        public async Task<IActionResult> ResetAdminPassword(string userId, CancellationToken cancellationToken)
        {
            return ProcessResponse(await _resetAdminPasswordService.ExecuteAsync(userId, cancellationToken));
        }

        [Authorize(Roles = RoleConstants.SuperAdmin)]
        [HttpGet("admin-roles")]
        public async Task<IActionResult> GetAdminRoles() => ProcessResponse(await _getAdminRolesService.ExecuteAsync());

        [Authorize(Roles = RoleConstants.SuperAdmin)]
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateAdminUser(string userId, [FromBody] UpdateAdminUserRequest request)
            => ProcessResponse(await _updateAdminUserService.ExecuteAsync(userId, request));

        [HttpGet("me")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Get the authenticated user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            var response = await _getUserByEmailService.ExecuteAsync(userId);
            return ProcessResponse(response);
        }

        [HttpPut("me")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ResponseResult<UserDetailsResponse>), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Update user details")]
        public async Task<IActionResult> Update([FromBody] UpdateUserDetailsRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            var response = await _updateUserService.ExecuteAsync(userId, request, cancellationToken);
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

        [HttpGet("me/addresses")]
        [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<UserAddressResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUserAddresses(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            return ProcessResponse(await _getAllUserAddressesService.ExecuteAsync(userId, cancellationToken));
        }

        [HttpPost("me/addresses")]
        [ProducesResponseType(typeof(ResponseResult<UserAddressResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUserAddress([FromBody] UserAddressRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            return ProcessResponse(await _createUserAddressService.ExecuteAsync(userId, request, cancellationToken));
        }

        [HttpPut("me/addresses/{addressId}")]
        [ProducesResponseType(typeof(ResponseResult<UserAddressResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserAddress(string addressId, [FromBody] UserAddressRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            return ProcessResponse(await _updateUserAddressService.ExecuteAsync(userId, addressId, request, cancellationToken));
        }

        [HttpDelete("me/addresses/{addressId}")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUserAddress(string addressId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            return ProcessResponse(await _deleteUserAddressService.ExecuteAsync(userId, addressId, cancellationToken));
        }
    }
}
