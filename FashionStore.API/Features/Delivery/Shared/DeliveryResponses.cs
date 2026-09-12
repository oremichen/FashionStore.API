namespace FashionStore.API.Features.Delivery.Shared;

public sealed record DeliveryRateResponse(
    string Id, 
    string ZoneId, 
    string ZoneName, 
    string MethodId, 
    string MethodName,
    long PriceKobo, 
    int? EstimatedDaysMin, 
    int? EstimatedDaysMax, 
    bool IsActive);

public sealed record DeliveryMethodResponse(
    string DeliveryId, 
    string ZoneId, 
    string ZoneName, 
    string MethodId,
    string MethodName, 
    long PriceKobo, 
    int? EstimatedDaysMin, 
    int? EstimatedDaysMax);
