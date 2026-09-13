using FashionStore.API.Features.Delivery.Shared;
using FashionStore.Domain.Abstractions.Delivery;

namespace FashionStore.API.Features.Delivery.GetMethods;

public sealed class GetMethodsService(IDeliveryRepository repository) : IGetMethodsService
{
    public async Task<ResponseResult<IReadOnlyList<DeliveryMethodCatalogResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var methods = await repository.GetMethodsAsync(cancellationToken);
        var result = new List<DeliveryMethodCatalogResponse>();

        foreach (var method in methods)
            result.Add(new DeliveryMethodCatalogResponse(method.Id, method.Name, method.IsActive));

        return new ResponseResult<IReadOnlyList<DeliveryMethodCatalogResponse>>().Success(result, "Delivery methods retrieved successfully.");
    }
}
