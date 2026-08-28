namespace FashionStore.API.Features.Users.UpdateUserAddress;
public interface IUpdateUserAddressService
{
    Task<ResponseResult<UserAddressResponse>> ExecuteAsync(string userId, string addressId, UserAddressRequest request, CancellationToken cancellationToken);
}
