using FashionStore.API.Features.Delivery.Shared;

namespace FashionStore.API.Features.Delivery.GetMethods;

public interface IGetMethodsService
{
    Task<ResponseResult<IReadOnlyList<DeliveryMethodCatalogResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
