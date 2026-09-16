using FashionStore.API.Features.Delivery.Shared;
using FashionStore.Domain.Abstractions.Delivery;

namespace FashionStore.API.Features.Delivery.GetRates;

public sealed class GetRatesService(IDeliveryRepository repository) : IGetRatesService
{
    public async Task<ResponseResult<IReadOnlyList<DeliveryRateResponse>>> ExecuteAsync(GetRatesRequest request, CancellationToken cancellationToken)
    {
        var rates = await repository.GetRatesAsync(false, cancellationToken);
        var result = new List<DeliveryRateResponse>();
        foreach (var rate in rates) result.Add(new DeliveryRateResponse(
            rate.Id, 
            rate.Zone.Id, 
            rate.Zone.Name, 
            rate.Method.Id, 
            rate.Method.Name, 
            rate.PriceKobo, 
            rate.EstimatedDaysMin, 
            rate.EstimatedDaysMax, 
            rate.IsActive));
        return new ResponseResult<IReadOnlyList<DeliveryRateResponse>>().Success(result, "Delivery rates retrieved successfully.");
    }
}
