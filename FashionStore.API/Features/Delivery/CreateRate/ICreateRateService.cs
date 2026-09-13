using FashionStore.API.Features.Delivery.Shared;

namespace FashionStore.API.Features.Delivery.CreateRate;

public interface ICreateRateService
{
    Task<ResponseResult<DeliveryRateResponse>> ExecuteAsync(CreateRateRequest request, CancellationToken cancellationToken);
}
