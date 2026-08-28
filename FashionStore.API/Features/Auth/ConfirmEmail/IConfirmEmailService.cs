namespace FashionStore.API.Features.Auth.ConfirmEmail;

public interface IConfirmEmailService
{
    Task<ResponseResult> ExecuteAsync(ConfirmEmailRequest request);
}
