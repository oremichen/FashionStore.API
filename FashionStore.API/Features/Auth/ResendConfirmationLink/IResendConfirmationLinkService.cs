namespace FashionStore.API.Features.Auth.ResendConfirmationLink;

public interface IResendConfirmationLinkService
{
    Task<ResponseResult> ExecuteAsync(ResendConfirmationLinkRequest request);
}
