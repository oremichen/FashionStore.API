namespace FashionStore.API.Features.Payments.Shared;

public sealed class DeliveryResolutionResult
{
    public bool IsPickup { get; set; }
    public string DeliveryMethodName { get; set; } = null!;
    public string DeliveryRateId { get; set; } = null!;
    public int? EstimatedDaysMin { get; set; }
    public int? EstimatedDaysMax { get; set; }
    public decimal DeliveryFee { get; set; }
    public string? ErrorMessage { get; set; }
}
