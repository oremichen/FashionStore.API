namespace FashionStore.Domain.Entities;

public sealed class DeliveryZone
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<DeliveryZoneLocation> Locations { get; set; } = new List<DeliveryZoneLocation>();
    public ICollection<DeliveryRate> Rates { get; set; } = new List<DeliveryRate>();
}

public sealed class DeliveryZoneLocation
{
    public string Id { get; set; } = null!;
    public string ZoneId { get; set; } = null!;
    public string State { get; set; } = null!;
    public DeliveryZone Zone { get; set; } = null!;
}

public sealed class DeliveryMethod
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<DeliveryRate> Rates { get; set; } = new List<DeliveryRate>();
}

public sealed class DeliveryRate
{
    public string Id { get; set; } = null!;
    public string ZoneId { get; set; } = null!;
    public string MethodId { get; set; } = null!;
    public long PriceKobo { get; set; }
    public int? EstimatedDaysMin { get; set; }
    public int? EstimatedDaysMax { get; set; }
    public bool IsActive { get; set; } = true;
    public DeliveryZone Zone { get; set; } = null!;
    public DeliveryMethod Method { get; set; } = null!;

    public void Update(
        string zoneId,
        string methodId,
        long priceKobo,
        int? estimatedDaysMin,
        int? estimatedDaysMax,
        bool isActive,
        DeliveryZone zone,
        DeliveryMethod method)
    {
        ZoneId = zoneId;
        MethodId = methodId;
        PriceKobo = priceKobo;
        EstimatedDaysMin = estimatedDaysMin;
        EstimatedDaysMax = estimatedDaysMax;
        IsActive = isActive;
        Zone = zone;
        Method = method;
    }
}
