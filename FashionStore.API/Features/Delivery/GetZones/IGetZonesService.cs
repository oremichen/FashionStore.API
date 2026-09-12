using FashionStore.API.Features.Delivery.Shared;

namespace FashionStore.API.Features.Delivery.GetZones;

public interface IGetZonesService
{
    Task<ResponseResult<IReadOnlyList<DeliveryZoneResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
