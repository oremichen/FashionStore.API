namespace FashionStore.API.Features.Users.GetAllUserAddresses;

public sealed class GetAllUserAddressesService(IUserRepository userRepository) : IGetAllUserAddressesService
{
    public async Task<ResponseResult<IReadOnlyList<UserAddressResponse>>> ExecuteAsync(string userId, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<IReadOnlyList<UserAddressResponse>>();
        if (!await userRepository.ExistsAsync(userId, cancellationToken))
            return response.Fail("User was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var addresses = (await userRepository.GetAddressesAsync(userId, false, cancellationToken))
            .Select(UserAddressResponse.From).ToList();
        return response.Success(addresses, "User addresses retrieved successfully.");
    }
}
