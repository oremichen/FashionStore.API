using FashionStore.Domain.Constants;

namespace FashionStore.Domain.Entities;

public sealed class Transaction
{
    private Transaction()
    {
    }

    public string Id { get; private set; } = null!;
    public string OrderId { get; private set; } = null!;
    public string? UserId { get; private set; }
    public string Reference { get; private set; } = null!;
    public string TransactionType { get; private set; } = TransactionTypes.Payment;
    public string Status { get; private set; } = TransactionStatuses.Pending;
    public string Provider { get; private set; } = null!;
    public string? ProviderTransactionId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public string? Channel { get; private set; }
    public string? GatewayResponseCode { get; private set; }
    public string? GatewayResponseMessage { get; private set; }
    public string? GatewayResponse { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Order Order { get; private set; } = null!;
    public ApplicationUser? User { get; private set; }
}
