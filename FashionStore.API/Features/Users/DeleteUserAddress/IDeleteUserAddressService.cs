namespace FashionStore.API.Features.Users.DeleteUserAddress;
public interface IDeleteUserAddressService
{
    Task<ResponseResult> ExecuteAsync(string userId, string addressId, CancellationToken cancellationToken);
}
