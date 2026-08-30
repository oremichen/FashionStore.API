namespace FashionStore.API.Features.Payments.Shared;

public sealed class CheckoutItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string? VariantId { get; set; }
    public int Quantity { get; set; }
}

public sealed class InitializePaystackRequest
{
    public string Email { get; set; } = string.Empty;
    public string AddressId { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = string.Empty;
    public IReadOnlyList<CheckoutItemRequest> Items { get; set; } = [];
}

public sealed record PaystackInitializationResponse(string AuthorizationUrl, string AccessCode, string Reference);
public sealed record PaymentVerificationResponse(string Reference, string OrderId, string Status);
