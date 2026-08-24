namespace FashionStore.API.Features.Auth.ResendConfirmationLink;

public sealed class ResendConfirmationLinkService(AuthOperations operations) : IResendConfirmationLinkService
{
    public Task<ResponseResult> ExecuteAsync(ResendConfirmationLinkRequest request) => operations.ResendConfirmationLink(request);
}
