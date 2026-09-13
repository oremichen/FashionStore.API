namespace FashionStore.API.Features.Users.GetPickupAddress;

public interface IGetPickupAddressService
{
    Task<ResponseResult<IReadOnlyList<UserAddressResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
