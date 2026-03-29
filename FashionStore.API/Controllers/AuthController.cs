using FashionStore.Application.Abstractions.Auth;
using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;
using FashionStore.Application.Features.Auth;
using FashionStore.Shared.Common;
using Microsoft.AspNetCore.Authorization;
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
        [AllowAnonymous]
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

        [HttpGet("Register")]
        public async Task<IActionResult> Register()
        {
            return Ok("This endpoint is accessible only to Admins.");
        }
    }
}
