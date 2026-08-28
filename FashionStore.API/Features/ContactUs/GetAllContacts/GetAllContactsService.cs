using FashionStore.API.Features.ContactUs.Shared;
using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.ContactUs.GetAllContacts;

public interface IGetAllContactsService
{
    Task<ResponseResult<IReadOnlyList<ContactUsResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class GetAllContactsService(IContactUsConfigurationRepository repository) : IGetAllContactsService
{
    public async Task<ResponseResult<IReadOnlyList<ContactUsResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var contacts = await repository.GetAllAsync(false, cancellationToken);
        var results = new List<ContactUsResponse>();
        foreach (var contact in contacts)
        {
            results.Add(ContactUsMapper.Map(contact));
        }

        return new ResponseResult<IReadOnlyList<ContactUsResponse>>()
            .Success(results, "Contacts retrieved successfully.");
    }
}
