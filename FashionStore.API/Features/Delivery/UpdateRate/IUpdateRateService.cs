using FashionStore.API.Features.Delivery.Shared;

namespace FashionStore.API.Features.Delivery.UpdateRate;

public interface IUpdateRateService
{
    Task<ResponseResult<DeliveryRateResponse>> ExecuteAsync(string id, UpdateRateRequest request, CancellationToken cancellationToken);
}
