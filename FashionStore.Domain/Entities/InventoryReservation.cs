using FashionStore.Domain.Constants;

namespace FashionStore.Domain.Entities;

public sealed class InventoryReservation
{
    private InventoryReservation() { }

    public string Id { get; private set; } = null!;
    public string OrderId { get; private set; } = null!;
    public Order Order { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public int Quantity { get; private set; }
    public string Status { get; private set; } = InventoryReservationStatuses.Reserved;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static InventoryReservation Create(string orderId, string productId, int quantity, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(productId)) throw new ArgumentException("Product id is required.", nameof(productId));
        if (quantity <= 0) throw new ArgumentException("Reserved quantity must be greater than zero.", nameof(quantity));
        if (expiresAt <= DateTimeOffset.UtcNow) throw new ArgumentException("Reservation expiry must be in the future.", nameof(expiresAt));
        return new InventoryReservation { OrderId = orderId.Trim(), ProductId = productId.Trim(), Quantity = quantity, ExpiresAt = expiresAt };
    }

    public void Consume(DateTimeOffset consumedAt)
    {
        EnsureReserved();
        Status = InventoryReservationStatuses.Consumed;
        ConsumedAt = consumedAt;
    }

    public void Release(DateTimeOffset releasedAt)
    {
        EnsureReserved();
        Status = InventoryReservationStatuses.Released;
        ReleasedAt = releasedAt;
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        EnsureReserved();
        if (expiredAt < ExpiresAt) throw new ArgumentException("A reservation cannot expire before its expiry time.", nameof(expiredAt));
        Status = InventoryReservationStatuses.Expired;
        ReleasedAt = expiredAt;
    }

    private void EnsureReserved()
    {
        if (Status != InventoryReservationStatuses.Reserved)
            throw new InvalidOperationException("Only reserved inventory can change status.");
    }
}
