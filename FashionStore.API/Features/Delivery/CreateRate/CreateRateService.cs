using FashionStore.API.Features.Delivery.Shared;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Entities;
using FashionStore.Shared.Constants;

namespace FashionStore.API.Features.Delivery.CreateRate;

public sealed class CreateRateService(IDeliveryRepository repository) : ICreateRateService
{
    public async Task<ResponseResult<DeliveryRateResponse>> ExecuteAsync(CreateRateRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request.ZoneId, request.MethodId, request.PriceKobo, request.EstimatedDaysMin, request.EstimatedDaysMax);
        if (validation is not null) return new ResponseResult<DeliveryRateResponse>().Fail(validation, ResponseCodes.INVALID_ACTION);
        var zone = await repository.GetZoneByIdAsync(request.ZoneId, cancellationToken);
        var method = await repository.GetMethodByIdAsync(request.MethodId, cancellationToken);
        if (zone is null || method is null) return new ResponseResult<DeliveryRateResponse>().Fail("The selected delivery zone or method was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (await repository.RateExistsAsync(request.ZoneId, request.MethodId, null, cancellationToken)) return new ResponseResult<DeliveryRateResponse>().Fail("A rate already exists for this zone and method.", ResponseCodes.DUPLICATE_RECORD);
        var rate = new DeliveryRate { Id = Guid.NewGuid().ToString(), ZoneId = zone.Id, MethodId = method.Id, PriceKobo = request.PriceKobo, EstimatedDaysMin = request.EstimatedDaysMin, EstimatedDaysMax = request.EstimatedDaysMax, IsActive = request.IsActive, Zone = zone, Method = method };
        await repository.AddRateAsync(rate, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new ResponseResult<DeliveryRateResponse>().Success(new DeliveryRateResponse(rate.Id, zone.Id, zone.Name, method.Id, method.Name, rate.PriceKobo, rate.EstimatedDaysMin, rate.EstimatedDaysMax, rate.IsActive), "Delivery rate created successfully.");
    }

    internal static string? Validate(string zoneId, string methodId, long priceKobo, int? minDays, int? maxDays)
    {
        if (string.IsNullOrWhiteSpace(zoneId) || string.IsNullOrWhiteSpace(methodId)) return "Zone and method are required.";
        if (priceKobo < 0) return "Delivery price cannot be negative.";
        if (minDays < 0 || maxDays < 0 || (minDays.HasValue && maxDays.HasValue && minDays > maxDays)) return "Estimated delivery days are invalid.";
        return null;
    }
}
