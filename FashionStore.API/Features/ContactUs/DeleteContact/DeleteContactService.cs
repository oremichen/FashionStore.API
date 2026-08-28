using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.ContactUs.DeleteContact;

public interface IDeleteContactService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}

public sealed class DeleteContactService(IContactUsConfigurationRepository repository) : IDeleteContactService
{
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        var response = new ResponseResult();
        if (string.IsNullOrWhiteSpace(id))
        {
            return response.Fail("Contact id is required.", ResponseCodes.INVALID_ACTION);
        }

        var contact = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (contact is null)
        {
            return response.Fail("Contact was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        await repository.DeleteAsync(contact, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return response.Success("Contact deleted successfully.");
    }
}
