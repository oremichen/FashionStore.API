using FashionStore.Domain.Constants;

namespace FashionStore.Domain.Entities;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];
    private Order() { }

    public string Id { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public ApplicationUser User { get; private set; } = null!;
    public string AddressId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string DeliveryMethod { get; private set; } = null!;
    public decimal Subtotal { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal Total { get; private set; }
    public string Currency { get; private set; } = "NGN";
    public string Status { get; private set; } = OrderStatuses.PendingPayment;
    public string PaymentReference { get; private set; } = null!;
    public string PaymentStatus { get; private set; } = PaymentStatuses.Pending;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items { get { return _items; } }

    public static Order Create(string userId, string addressId, string email, string deliveryMethod,
        decimal subtotal, decimal deliveryFee, string paymentReference, IEnumerable<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User id is required.");
        if (string.IsNullOrWhiteSpace(addressId)) throw new ArgumentException("Address id is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        if (subtotal < 0 || deliveryFee < 0) throw new ArgumentException("Order amounts cannot be negative.");

        var order = new Order
        {
            UserId = userId.Trim(), AddressId = addressId.Trim(), Email = email.Trim(),
            DeliveryMethod = deliveryMethod, Subtotal = subtotal, DeliveryFee = deliveryFee,
            Total = subtotal + deliveryFee, PaymentReference = paymentReference
        };
        order._items.AddRange(items);
        if (order._items.Count == 0) throw new ArgumentException("An order must contain at least one item.");
        return order;
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        if (PaymentStatus == PaymentStatuses.Success) return;
        PaymentStatus = PaymentStatuses.Success;
        Status = OrderStatuses.Processing;
        PaidAt = paidAt;
    }

    public void MarkPaymentFailed(string status)
    {
        if (PaymentStatus == PaymentStatuses.Success) return;
        PaymentStatus = string.IsNullOrWhiteSpace(status)
            ? PaymentStatuses.Failed
            : status.Trim().ToLowerInvariant();
    }
}
