using FashionStore.API.Features.Delivery.Shared;

namespace FashionStore.API.Features.Delivery.GetDeliveryMethods;

public interface IGetDeliveryMethodsService
{
    Task<ResponseResult<IReadOnlyList<DeliveryMethodResponse>>> ExecuteAsync(GetDeliveryMethodsRequest request, CancellationToken cancellationToken);
}
