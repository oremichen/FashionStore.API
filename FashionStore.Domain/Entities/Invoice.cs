using FashionStore.Domain.Constants;

namespace FashionStore.Domain.Entities;

public sealed class Invoice
{
    private Invoice()
    {
    }

    public string Id { get; private set; } = null!;
    public string OrderId { get; private set; } = null!;
    public string? UserId { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;
    public string Status { get; private set; } = InvoiceStatuses.Issued;
    public string? CustomerName { get; private set; }
    public string CustomerEmail { get; private set; } = null!;
    public string? BillingAddress { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = null!;
    public string LineItems { get; private set; } = null!;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? PdfUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Order Order { get; private set; } = null!;
    public ApplicationUser? User { get; private set; }
}
