using FashionStore.Domain.Constants;

namespace FashionStore.Domain.Entities;

public sealed class Order
{
    private static readonly Random _random = new();
    private readonly List<OrderItem> _items = [];
    private readonly List<InventoryReservation> _inventoryReservations = [];
    private Order() { }

    public string Id { get; private set; } = null!;
    public string TrackOrderId { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public ApplicationUser User { get; private set; } = null!;
    public string AddressId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string DeliveryMethod { get; private set; } = null!;
    public string DeliveryRateId { get; private set; } = null!;
    public int? EstimatedDaysMin { get; private set; }
    public int? EstimatedDaysMax { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal Total { get; private set; }
    public string Currency { get; private set; } = "NGN";
    public string Status { get; private set; } = OrderStatuses.PendingPayment;
    public string? RefundedReason { get; private set; }
    public string PaymentReference { get; private set; } = null!;
    public string PaymentProvider { get; private set; } = PaymentProviderKeys.Paystack;
    public string? AuthorizationUrl { get; private set; }
    public string PaymentStatus { get; private set; } = PaymentStatuses.Pending;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items { get { return _items; } }
    public IReadOnlyCollection<InventoryReservation> InventoryReservations { get { return _inventoryReservations; } }

    private static string GenerateTrackOrderId()
    {
        int number;
        lock (_random)
        {
            number = _random.Next(1000000, 10000000);
        }
        return $"#order{number}";
    }

    public static Order Create(string userId, string idempotencyKey, string addressId, string email, string deliveryMethod,
        string deliveryRateId, int? estimatedDaysMin, int? estimatedDaysMax,
        decimal subtotal, decimal deliveryFee, string paymentReference, IEnumerable<OrderItem> items,
        string? paymentProvider = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User id is required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.");
        if (string.IsNullOrWhiteSpace(addressId)) throw new ArgumentException("Address id is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(deliveryRateId)) throw new ArgumentException("Delivery rate is required.");
        if (subtotal < 0 || deliveryFee < 0) throw new ArgumentException("Order amounts cannot be negative.");

        var normalizedProvider = string.IsNullOrWhiteSpace(paymentProvider)
            ? PaymentProviderKeys.Paystack
            : paymentProvider.Trim().ToLowerInvariant();

        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            TrackOrderId = GenerateTrackOrderId(),
            UserId = userId.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            AddressId = addressId.Trim(),
            Email = email.Trim(),
            DeliveryMethod = deliveryMethod,
            DeliveryRateId = deliveryRateId.Trim(),
            EstimatedDaysMin = estimatedDaysMin,
            EstimatedDaysMax = estimatedDaysMax,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Total = subtotal + deliveryFee,
            PaymentReference = paymentReference,
            PaymentProvider = normalizedProvider
        };
        order._items.AddRange(items);
        if (order._items.Count == 0) throw new ArgumentException("An order must contain at least one item.");
        return order;
    }

    public void ReserveInventory(string productId, int quantity, DateTimeOffset expiresAt)
    {
        foreach (var reservation in _inventoryReservations)
        {
            if (reservation.ProductId == productId)
                throw new ArgumentException("Only one reservation is allowed per product on an order.", nameof(productId));
        }
        _inventoryReservations.Add(InventoryReservation.Create(Id, productId, quantity, expiresAt));
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        if (PaymentStatus == PaymentStatuses.Success) return;
        PaymentStatus = PaymentStatuses.Success;
        Status = OrderStatuses.Processing;
        PaidAt = paidAt;
    }

    public void SetAuthorizationUrl(string authorizationUrl)
    {
        if (!Uri.TryCreate(authorizationUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("A valid payment authorization URL is required.", nameof(authorizationUrl));
        AuthorizationUrl = uri.ToString();
    }

    public void MarkPaymentFailed(string status)
    {
        if (PaymentStatus == PaymentStatuses.Success) return;
        PaymentStatus = string.IsNullOrWhiteSpace(status)
            ? PaymentStatuses.Failed
            : status.Trim().ToLowerInvariant();
    }

    public void UpdateStatus(string status, string? statusMessage = null)
    {
        var normalized = status?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized is not (OrderStatuses.Processing or OrderStatuses.Shipped or OrderStatuses.Delivered or OrderStatuses.Cancelled or OrderStatuses.Returned))
            throw new ArgumentException("Unsupported order status.", nameof(status));
        if (Status == OrderStatuses.Processing && normalized is not (OrderStatuses.Cancelled or OrderStatuses.Shipped))
            throw new ArgumentException("A processing order can only be changed to Cancelled or Shipped.", nameof(status));
        if (Status == OrderStatuses.Shipped && normalized != OrderStatuses.Delivered)
            throw new ArgumentException("A shipped order can only be changed to Delivered.", nameof(status));
        if (Status == OrderStatuses.Delivered && normalized != OrderStatuses.Returned)
            throw new ArgumentException("A delivered order can only be changed to Returned.", nameof(status));
        if (Status is OrderStatuses.Cancelled or OrderStatuses.Returned)
            throw new ArgumentException("This order is already closed and cannot change status.", nameof(status));
        if (normalized is OrderStatuses.Cancelled or OrderStatuses.Returned)
        {
            if (string.IsNullOrWhiteSpace(statusMessage)) throw new ArgumentException("A message is required when cancelling or returning an order.", nameof(statusMessage));
            if (statusMessage.Trim().Length > 1024) throw new ArgumentException("The status message cannot exceed 1024 characters.", nameof(statusMessage));
            RefundedReason = statusMessage.Trim();
            if (PaymentStatus == PaymentStatuses.Success)
                PaymentStatus = PaymentStatuses.Refunded;
        }
        if (normalized == OrderStatuses.Delivered &&
            PaymentProvider.Equals(PaymentProviderKeys.PayOnDelivery, StringComparison.OrdinalIgnoreCase) &&
            PaymentStatus != PaymentStatuses.Success &&
            PaymentStatus != PaymentStatuses.Refunded)
        {
            PaymentStatus = PaymentStatuses.Success;
            PaidAt = DateTimeOffset.UtcNow;
        }
        Status = normalized;
    }
}
