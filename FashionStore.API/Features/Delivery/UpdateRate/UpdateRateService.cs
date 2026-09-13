using FashionStore.API.Features.Delivery.CreateRate;
using FashionStore.API.Features.Delivery.Shared;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Shared.Constants;

namespace FashionStore.API.Features.Delivery.UpdateRate;

public sealed class UpdateRateService(IDeliveryRepository repository) : IUpdateRateService
{
    public async Task<ResponseResult<DeliveryRateResponse>> ExecuteAsync(string id, UpdateRateRequest request, CancellationToken cancellationToken)
    {
        var validation = CreateRateService.Validate(request.ZoneId, request.MethodId, request.PriceKobo, request.EstimatedDaysMin, request.EstimatedDaysMax);
        if (validation is not null) return new ResponseResult<DeliveryRateResponse>().Fail(validation, ResponseCodes.INVALID_ACTION);
        var rate = await repository.GetRateByIdAsync(id, cancellationToken);
        if (rate is null) return new ResponseResult<DeliveryRateResponse>().Fail("Delivery rate was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        var zone = await repository.GetZoneByIdAsync(request.ZoneId, cancellationToken);
        var method = await repository.GetMethodByIdAsync(request.MethodId, cancellationToken);
        if (zone is null || method is null) return new ResponseResult<DeliveryRateResponse>().Fail("The selected delivery zone or method was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (await repository.RateExistsAsync(request.ZoneId, request.MethodId, id, cancellationToken)) return new ResponseResult<DeliveryRateResponse>().Fail("A rate already exists for this zone and method.", ResponseCodes.DUPLICATE_RECORD);
        rate.ZoneId = request.ZoneId; rate.MethodId = request.MethodId; rate.PriceKobo = request.PriceKobo; rate.EstimatedDaysMin = request.EstimatedDaysMin; rate.EstimatedDaysMax = request.EstimatedDaysMax; rate.IsActive = request.IsActive; rate.Zone = zone; rate.Method = method;
        await repository.SaveChangesAsync(cancellationToken);
        return new ResponseResult<DeliveryRateResponse>().Success(new DeliveryRateResponse(rate.Id, zone.Id, zone.Name, method.Id, method.Name, rate.PriceKobo, rate.EstimatedDaysMin, rate.EstimatedDaysMax, rate.IsActive), "Delivery rate updated successfully.");
    }
}
