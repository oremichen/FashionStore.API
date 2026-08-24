namespace FashionStore.API.Features.Users.GetAllUserAddresses;

public interface IGetAllUserAddressesService
{
    Task<ResponseResult<IReadOnlyList<UserAddressResponse>>> ExecuteAsync(string userId, CancellationToken cancellationToken);
}
