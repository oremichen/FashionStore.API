namespace FashionStore.API.Features.Users.UpdateUserAddress;

public sealed class UpdateUserAddressService(IUserRepository userRepository) : IUpdateUserAddressService
{
    public async Task<ResponseResult<UserAddressResponse>> ExecuteAsync(string userId, string addressId, UserAddressRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<UserAddressResponse>();
        var address = await userRepository.GetAddressAsync(userId, addressId, cancellationToken);
        if (address is null)
            return response.Fail("User address was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var others = (await userRepository.GetAddressesAsync(userId, true, cancellationToken))
            .Where(item => item.Id != addressId);
        try
        {
            address.Update(request.Street, request.City, request.State, request.Country,
                request.PostalCode, request.PhoneNumber, request.Landmark, request.IsMain, others);
            await userRepository.SaveChangesAsync(cancellationToken);
            return response.Success(UserAddressResponse.From(address), "User address updated successfully.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }
}
