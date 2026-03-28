using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Features.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FashionStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
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
