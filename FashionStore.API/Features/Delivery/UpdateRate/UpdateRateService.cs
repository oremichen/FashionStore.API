using FashionStore.API.Features.Delivery.CreateRate;
using FashionStore.API.Features.Delivery.Shared;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Entities;
using FashionStore.Shared.Constants;

namespace FashionStore.API.Features.Delivery.UpdateRate;

public sealed class UpdateRateService(IDeliveryRepository repository) : IUpdateRateService
{
    public async Task<ResponseResult<DeliveryRateResponse>> ExecuteAsync(
        string id,
        UpdateRateRequest request,
        CancellationToken cancellationToken)
    {
        var response = new ResponseResult<DeliveryRateResponse>();

        var validationMessage = CreateRateService.Validate(
            request.ZoneId,
            request.MethodId,
            request.PriceKobo,
            request.EstimatedDaysMin,
            request.EstimatedDaysMax);

        if (validationMessage is not null)
        {
            return response.Fail(validationMessage, ResponseCodes.INVALID_ACTION);
        }

        var rate = await repository.GetRateByIdAsync(id, cancellationToken);
        if (rate is null)
        {
            return response.Fail("Delivery rate was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var lookupResult = await LookupZoneAndMethodAsync(request, cancellationToken);
        if (lookupResult is null)
        {
            return response.Fail(
                "The selected delivery zone or method was not found.",
                ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var (zone, method) = lookupResult.Value;

        var isDuplicate = await repository.RateExistsAsync(
            request.ZoneId,
            request.MethodId,
            id,
            cancellationToken);

        if (isDuplicate)
        {
            return response.Fail(
                $"A delivery rate already exists for zone \"{zone.Name}\" and method \"{method.Name}\". " +
                "Each zone-method combination can only have one rate. Please update the existing rate instead.",
                ResponseCodes.DUPLICATE_RECORD);
        }

        ApplyRequest(rate, request, zone, method);
        await repository.SaveChangesAsync(cancellationToken);

        return response.Success(
            BuildResponse(rate, zone, method),
            "Delivery rate updated successfully.");
    }

    private async Task<(DeliveryZone Zone, DeliveryMethod Method)?> LookupZoneAndMethodAsync(
        UpdateRateRequest request,
        CancellationToken cancellationToken)
    {
        var zone = await repository.GetZoneByIdAsync(request.ZoneId, cancellationToken);
        var method = await repository.GetMethodByIdAsync(request.MethodId, cancellationToken);

        if (zone is null || method is null)
        {
            return null;
        }

        return (zone, method);
    }

    private static void ApplyRequest(
        DeliveryRate rate,
        UpdateRateRequest request,
        DeliveryZone zone,
        DeliveryMethod method)
    {
        rate.Update(
            zoneId: request.ZoneId,
            methodId: request.MethodId,
            priceKobo: request.PriceKobo,
            estimatedDaysMin: request.EstimatedDaysMin,
            estimatedDaysMax: request.EstimatedDaysMax,
            isActive: request.IsActive,
            zone: zone,
            method: method);
    }

    private static DeliveryRateResponse BuildResponse(
        DeliveryRate rate,
        DeliveryZone zone,
        DeliveryMethod method)
    {
        return new DeliveryRateResponse(
            Id: rate.Id,
            ZoneId: zone.Id,
            ZoneName: zone.Name,
            MethodId: method.Id,
            MethodName: method.Name,
            PriceKobo: rate.PriceKobo,
            EstimatedDaysMin: rate.EstimatedDaysMin,
            EstimatedDaysMax: rate.EstimatedDaysMax,
            IsActive: rate.IsActive);
    }
}
