using FashionStore.Application.Abstractions.Auth;
using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;
using FashionStore.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FashionStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult<LoginResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult<LoginResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseResult<LoginResponse>), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Authenticate user")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            var response = await _authService.Login(login);
            return ProcessResponse(response);
        }

        [Authorize]
        [HttpPost("logout")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Logout user")]
        public async Task<IActionResult> Logout()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;
            var tokenId = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;

            var response = await _authService.Logout(username, tokenId);
            return ProcessResponse(response);
        }

        [HttpPost("register")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Register user")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.Register(request);
            return ProcessResponse(response);
        }

        [HttpPost("confirm-email")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Confirm user email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            var response = await _authService.ConfirmEmail(request);
            return ProcessResponse(response);
        }

        [HttpPost("resend-confirmation-link")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Resend confirmation email")]
        public async Task<IActionResult> ResendConfirmationLink([FromBody] ResendConfirmationLinkRequest request)
        {
            var response = await _authService.ResendConfirmationLink(request);
            return ProcessResponse(response);
        }
    }
}
