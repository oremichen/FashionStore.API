using FashionStore.API.Features.ContactUs.Shared;

namespace FashionStore.API.Features.ContactUs.CreateContact;

public interface ICreateContactService
{
    Task<ResponseResult<ContactUsResponse>> ExecuteAsync(ContactUsRequest request, CancellationToken cancellationToken);
}
