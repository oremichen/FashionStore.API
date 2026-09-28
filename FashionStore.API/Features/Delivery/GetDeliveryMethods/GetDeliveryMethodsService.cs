using FashionStore.API.Features.Delivery.Shared;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Shared.Constants;

namespace FashionStore.API.Features.Delivery.GetDeliveryMethods;

public sealed class GetDeliveryMethodsService(IDeliveryRepository repository) : IGetDeliveryMethodsService
{
    public async Task<ResponseResult<IReadOnlyList<DeliveryMethodResponse>>> ExecuteAsync(GetDeliveryMethodsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.State))
        {
            return new ResponseResult<IReadOnlyList<DeliveryMethodResponse>>().Fail("State is required.", ResponseCodes.INVALID_ACTION);
        }
        
        var rates = await repository.GetActiveRatesForStateAsync(request.State.Trim(), cancellationToken);
        
        var result = new List<DeliveryMethodResponse>();
       
        foreach (var rate in rates)
        {
            result.Add(new DeliveryMethodResponse(
                rate.Id, 
                rate.Zone.Id, 
                rate.Zone.Name, 
                rate.Method.Id, 
                rate.Method.Name, 
                request.IncludeRates is false ? 0 : rate.PriceKobo,
                rate.EstimatedDaysMin, 
                rate.EstimatedDaysMax));
        }
        
        return new ResponseResult<IReadOnlyList<DeliveryMethodResponse>>().Success(result, "Delivery methods retrieved successfully.");
    }
}
