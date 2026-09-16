namespace FashionStore.API.Features.Delivery.CreateRate;

public sealed class CreateRateRequest
{
    public string ZoneId { get; init; } = string.Empty;
    public string MethodId { get; init; } = string.Empty;
    public long PriceKobo { get; init; }
    public int? EstimatedDaysMin { get; init; }
    public int? EstimatedDaysMax { get; init; }
    public bool IsActive { get; init; } = true;
}
