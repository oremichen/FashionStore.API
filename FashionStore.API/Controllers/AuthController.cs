using Microsoft.AspNetCore.Mvc;

namespace FashionStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        [HttpGet("login")]
        public async Task<IActionResult> Login()
        {
            return Ok("This endpoint is accessible only to Admins.");
        }

        [HttpGet("Register")]
        public async Task<IActionResult> Register()
        {
            return Ok("This endpoint is accessible only to Admins.");
        }
    }
}
