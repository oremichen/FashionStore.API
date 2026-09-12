using FashionStore.API.Features.Delivery.Shared;
using FashionStore.Domain.Abstractions.Delivery;

namespace FashionStore.API.Features.Delivery.GetZones;

public sealed class GetZonesService(IDeliveryRepository repository) : IGetZonesService
{
    public async Task<ResponseResult<IReadOnlyList<DeliveryZoneResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var zones = await repository.GetZonesAsync(cancellationToken);
        var result = new List<DeliveryZoneResponse>();

        foreach (var zone in zones)
            result.Add(new DeliveryZoneResponse(zone.Id, zone.Name, zone.IsDefault, zone.IsActive));

        return new ResponseResult<IReadOnlyList<DeliveryZoneResponse>>().Success(result, "Delivery zones retrieved successfully.");
    }
}
