using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.Users.GetPickupAddress;

public sealed class GetPickupAddressService(IContactUsConfigurationRepository repository) : IGetPickupAddressService
{
    public async Task<ResponseResult<UserAddressResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var response = new ResponseResult<UserAddressResponse>();
        var contact = await repository.GetActiveAsync(cancellationToken);
        if (contact?.AddressDetails is null)
        {
            return response.Fail("There is no active pickup address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        contact.AddressDetails.PhoneNumber = contact.ContactPhone;
        return response.Success(UserAddressResponse.From(contact.AddressDetails), "Pickup address retrieved successfully.");
    }
}
