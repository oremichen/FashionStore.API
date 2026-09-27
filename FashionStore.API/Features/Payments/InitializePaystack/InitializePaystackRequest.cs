namespace FashionStore.API.Features.Payments.InitializePaystack;

public class InitializePaystackRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressId { get; set; } = string.Empty;
    public string DeliveryId { get; set; } = string.Empty;
    public string CartId { get; set; } = string.Empty;
}
