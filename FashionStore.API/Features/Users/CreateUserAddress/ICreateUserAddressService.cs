namespace FashionStore.API.Features.Users.CreateUserAddress;
public interface ICreateUserAddressService
{
    Task<ResponseResult<UserAddressResponse>> ExecuteAsync(string userId, UserAddressRequest request, CancellationToken cancellationToken);
}
