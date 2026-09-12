using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Delivery;

public interface IDeliveryRepository
{
    Task<IReadOnlyList<DeliveryRate>> GetRatesAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeliveryRate>> GetActiveRatesForStateAsync(string state, CancellationToken cancellationToken);
    Task<DeliveryRate?> GetActiveRateForStateAsync(string id, string state, CancellationToken cancellationToken);
    Task<DeliveryRate?> GetRateByIdAsync(string id, CancellationToken cancellationToken);
    Task<DeliveryZone?> GetZoneByIdAsync(string id, CancellationToken cancellationToken);
    Task<DeliveryMethod?> GetMethodByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> RateExistsAsync(string zoneId, string methodId, string? excludingId, CancellationToken cancellationToken);
    Task AddRateAsync(DeliveryRate rate, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
