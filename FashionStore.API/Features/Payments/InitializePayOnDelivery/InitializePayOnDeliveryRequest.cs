using FashionStore.API.Features.Payments.Shared;

namespace FashionStore.API.Features.Payments.InitializePayOnDelivery;

public sealed class InitializePayOnDeliveryRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressId { get; set; } = string.Empty;
    public string DeliveryId { get; set; } = string.Empty;
    public IReadOnlyList<CheckoutItemRequest> Items { get; set; } = [];
}
