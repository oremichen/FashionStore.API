namespace FashionStore.API.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailService(AuthOperations operations) : IConfirmEmailService
{
    public Task<ResponseResult> ExecuteAsync(ConfirmEmailRequest request) => operations.ConfirmEmail(request);
}
