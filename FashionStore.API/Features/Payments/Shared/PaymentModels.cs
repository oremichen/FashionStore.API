namespace FashionStore.API.Features.Payments.Shared;

public sealed class CheckoutItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string? VariantId { get; set; }
    public string? ColorId { get; set; }
    public int Quantity { get; set; }
}
