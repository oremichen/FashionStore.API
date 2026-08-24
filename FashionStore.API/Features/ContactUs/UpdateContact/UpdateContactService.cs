using FashionStore.API.Features.ContactUs.Shared;
using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.ContactUs.UpdateContact;

public interface IUpdateContactService
{
    Task<ResponseResult<ContactUsResponse>> ExecuteAsync(string id, ContactUsRequest request, CancellationToken cancellationToken);
}

public sealed class UpdateContactService(IContactUsRepository repository) : IUpdateContactService
{
    public async Task<ResponseResult<ContactUsResponse>> ExecuteAsync(string id, ContactUsRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<ContactUsResponse>();
        if (string.IsNullOrWhiteSpace(id))
        {
            return response.Fail("Contact id is required.", ResponseCodes.INVALID_ACTION);
        }

        var contact = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (contact is null)
        {
            return response.Fail("Contact was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        try
        {
            if (request.IsActive)
            {
                await DeactivateOtherContactsAsync(contact.Id, cancellationToken);
            }

            contact.Update(request.Address, request.ContactPhone, request.BusinessPhone,
                request.ContactEmail, request.BusinessEmail, request.IsActive);
            await repository.SaveChangesAsync(cancellationToken);
            return response.Success(ContactUsMapper.Map(contact), "Contact updated successfully.");
        }
        catch (ArgumentException exception)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private async Task DeactivateOtherContactsAsync(string excludedId, CancellationToken cancellationToken)
    {
        var contacts = await repository.GetAllAsync(true, cancellationToken);
        foreach (var item in contacts)
        {
            if (item.IsActive && item.Id != excludedId)
            {
                item.Deactivate();
            }
        }
    }
}
