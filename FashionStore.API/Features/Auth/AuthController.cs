using FashionStore.API.Features.Auth.ConfirmEmail;
using FashionStore.API.Features.Auth.ForgotPassword;
using FashionStore.API.Features.Auth.Login;
using FashionStore.API.Features.Auth.Logout;
using FashionStore.API.Features.Auth.Register;
using FashionStore.API.Features.Auth.ResendConfirmationLink;
using FashionStore.API.Features.Auth.ResetPassword;

namespace FashionStore.API.Features.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly ILoginService _loginService;
        private readonly ILogoutService _logoutService;
        private readonly IForgotPasswordService _forgotPasswordService;
        private readonly IResetPasswordService _resetPasswordService;
        private readonly IRegisterService _registerService;
        private readonly IConfirmEmailService _confirmEmailService;
        private readonly IResendConfirmationLinkService _resendConfirmationLinkService;

        public AuthController(ILoginService loginService, ILogoutService logoutService, IForgotPasswordService forgotPasswordService, IResetPasswordService resetPasswordService, IRegisterService registerService, IConfirmEmailService confirmEmailService, IResendConfirmationLinkService resendConfirmationLinkService)
        {
            _loginService = loginService;
            _logoutService = logoutService;
            _forgotPasswordService = forgotPasswordService;
            _resetPasswordService = resetPasswordService;
            _registerService = registerService;
            _confirmEmailService = confirmEmailService;
            _resendConfirmationLinkService = resendConfirmationLinkService;
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
            var response = await _loginService.ExecuteAsync(login);
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

            var response = await _logoutService.ExecuteAsync(username, tokenId);
            return ProcessResponse(response);
        }

        [HttpPost("forgot-password")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Generate temporary password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var response = await _forgotPasswordService.ExecuteAsync(request);
            return ProcessResponse(response);
        }

        [Authorize]
        [HttpPost("reset-password")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Reset password for authenticated user")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;

            var response = await _resetPasswordService.ExecuteAsync(username, request);
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
            var response = await _registerService.ExecuteAsync(request);
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
            var response = await _confirmEmailService.ExecuteAsync(request);
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
            var response = await _resendConfirmationLinkService.ExecuteAsync(request);
            return ProcessResponse(response);
        }
    }
}
