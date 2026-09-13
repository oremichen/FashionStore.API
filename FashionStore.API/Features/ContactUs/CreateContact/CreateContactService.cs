using FashionStore.API.Features.ContactUs.Shared;
using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.ContactUs.CreateContact;

public sealed class CreateContactService(IContactUsConfigurationRepository repository) : ICreateContactService
{
    public async Task<ResponseResult<ContactUsResponse>> ExecuteAsync(ContactUsRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<ContactUsResponse>();
        try
        {
            if (request.IsActive)
            {
                await DeactivateOtherContactsAsync(null, cancellationToken);
            }

            var address = FashionStore.Domain.Entities.Address.CreateForContact(
                request.Country, 
                request.State, 
                request.ContactPhone, 
                request.City, 
                request.Street);

            address.Id = Guid.NewGuid().ToString();
            await repository.AddAddressAsync(address, cancellationToken);

            var contact = FashionStore.Domain.Entities.ContactUsConfiguration.Create(
                address.Id, 
                request.ContactPhone, 
                request.BusinessPhone,
                request.ContactEmail, 
                request.BusinessEmail, 
                request.IsActive);
            await repository.AddAsync(contact, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return response.Success(ContactUsMapper.Map(contact), "Contact created successfully.")
                .SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private async Task DeactivateOtherContactsAsync(string? excludedId, CancellationToken cancellationToken)
    {
        var contacts = await repository.GetAllAsync(true, cancellationToken);
        foreach (var contact in contacts)
        {
            if (contact.IsActive && contact.Id != excludedId)
            {
                contact.Deactivate();
            }
        }
    }
}
