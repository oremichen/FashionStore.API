using FashionStore.API.Features.Payments.Shared;

namespace FashionStore.API.Features.Payments.InitializePaystack;

public interface IInitializePaystackService
{
    Task<ResponseResult<PaystackInitializationResponse>> ExecuteAsync(string userId, InitializePaystackRequest request, CancellationToken cancellationToken);
}
