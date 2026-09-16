using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Shared.Constants;

namespace FashionStore.API.Features.Delivery.DeleteRate;

public sealed class DeleteRateService(IDeliveryRepository repository) : IDeleteRateService
{
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        var response = new ResponseResult();
        if (string.IsNullOrWhiteSpace(id))
        {
            return response.Fail("Delivery rate id is required.", ResponseCodes.INVALID_ACTION);
        }

        var rateId = id.Trim();
        var rate = await repository.GetRateByIdAsync(rateId, cancellationToken);
        if (rate is null)
        {
            return response.Fail("Delivery rate was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        await repository.DeleteRateAsync(rate, cancellationToken);
        return response.Success("Delivery rate deleted successfully.");
    }
}
