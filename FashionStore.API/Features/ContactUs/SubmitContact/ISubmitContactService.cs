namespace FashionStore.API.Features.ContactUs.SubmitContact;

public interface ISubmitContactService
{
    Task<ResponseResult> ExecuteAsync(SubmitContactRequest request, CancellationToken cancellationToken);
}
