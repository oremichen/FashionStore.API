namespace FashionStore.API.Features.Payments.InitializePayOnDelivery;

public sealed record PayOnDeliveryInitializationResponse(
    string OrderId,
    string TrackOrderId,
    string Reference,
    string Status);
