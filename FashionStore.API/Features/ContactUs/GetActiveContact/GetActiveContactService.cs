using FashionStore.API.Features.ContactUs.Shared;
using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.ContactUs.GetActiveContact;

public interface IGetActiveContactService
{
    Task<ResponseResult<ContactUsResponse>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class GetActiveContactService(IContactUsRepository repository) : IGetActiveContactService
{
    public async Task<ResponseResult<ContactUsResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var response = new ResponseResult<ContactUsResponse>();
        var contact = await repository.GetActiveAsync(cancellationToken);
        if (contact is null)
        {
            return response.Fail("There is no active contact.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        return response.Success(ContactUsMapper.Map(contact), "Active contact retrieved successfully.");
    }
}
