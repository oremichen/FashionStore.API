using FashionStore.API.Features.Payments.Shared;

namespace FashionStore.API.Features.Payments.InitializePayOnDelivery;

public interface IInitializePayOnDeliveryService
{
    Task<ResponseResult<PayOnDeliveryInitializationResponse>> ExecuteAsync(
        string userId,
        InitializePayOnDeliveryRequest request,
        CancellationToken cancellationToken);
}
