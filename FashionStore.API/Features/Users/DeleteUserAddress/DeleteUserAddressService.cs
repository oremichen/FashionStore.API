namespace FashionStore.API.Features.Users.DeleteUserAddress;
public sealed class DeleteUserAddressService(IUserRepository userRepository) : IDeleteUserAddressService
{
    public async Task<ResponseResult> ExecuteAsync(string userId, string addressId, CancellationToken cancellationToken)
    {
        var address = await userRepository.GetAddressAsync(userId, addressId, cancellationToken);
        if (address is null)
            return new ResponseResult().Fail("User address was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        userRepository.DeleteAddress(address);
        await userRepository.SaveChangesAsync(cancellationToken);
        return new ResponseResult().Success("User address deleted successfully.");
    }
}
