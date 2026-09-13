using FashionStore.API.Features.Delivery.Shared;

namespace FashionStore.API.Features.Delivery.GetRates;

public interface IGetRatesService
{
    Task<ResponseResult<IReadOnlyList<DeliveryRateResponse>>> ExecuteAsync(GetRatesRequest request, CancellationToken cancellationToken);
}
