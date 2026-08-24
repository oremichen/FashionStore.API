namespace FashionStore.API.Features.Users.CreateUserAddress;
public sealed class CreateUserAddressService(IUserRepository userRepository) : ICreateUserAddressService
{
    public async Task<ResponseResult<UserAddressResponse>> ExecuteAsync(string userId, UserAddressRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<UserAddressResponse>();
        if (!await userRepository.ExistsAsync(userId, cancellationToken))
            return response.Fail("User was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        var existing = await userRepository.GetAddressesAsync(userId, true, cancellationToken);
        try
        {
            var address = Address.Create(userId, request.Street, request.City, request.State,
                request.Country, request.PostalCode, request.PhoneNumber, request.Landmark,
                request.IsMain, existing);
            userRepository.AddAddress(address);
            await userRepository.SaveChangesAsync(cancellationToken);
            return response.Success(UserAddressResponse.From(address), "User address created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }
}
