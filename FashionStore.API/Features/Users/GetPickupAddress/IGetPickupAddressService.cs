namespace FashionStore.API.Features.Users.GetPickupAddress;

public interface IGetPickupAddressService
{
    Task<ResponseResult<UserAddressResponse>> ExecuteAsync(CancellationToken cancellationToken);
}
