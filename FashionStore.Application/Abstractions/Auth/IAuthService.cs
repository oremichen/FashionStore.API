using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;
using FashionStore.Shared.Common;

namespace FashionStore.Application.Abstractions.Auth
{
    public interface IAuthService
    {
        Task<ResponseResult<LoginResponse>> Login(LoginRequest login);
        Task<ResponseResult> Logout(string username, string tokenId);
        Task<ResponseResult> ForgotPassword(ForgotPasswordRequest request);
        Task<ResponseResult> Register(RegisterRequest request);
        Task<ResponseResult> ConfirmEmail(ConfirmEmailRequest request);
        Task<ResponseResult> ResendConfirmationLink(ResendConfirmationLinkRequest request);
    }
}
