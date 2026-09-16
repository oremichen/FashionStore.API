using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.Users.GetPickupAddress;

public sealed class GetPickupAddressService(IUserRepository repository) : IGetPickupAddressService
{
    public async Task<ResponseResult<IReadOnlyList<UserAddressResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var response = new ResponseResult<IReadOnlyList<UserAddressResponse>>();
        var addresses = await repository.GetSystemAddressesAsync(cancellationToken);

        if (addresses is null || addresses.Count == 0)
        {
            return response.Fail("There is no active pickup address.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var userAddressResponses = addresses.Select(UserAddressResponse.From).ToList();
        return response.Success(userAddressResponses, "Pickup addresses retrieved successfully.");
    }
}
