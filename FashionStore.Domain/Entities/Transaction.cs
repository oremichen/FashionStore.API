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

    public static Transaction CreateSuccessfulPayment(
        string orderId,
        string? userId,
        string reference,
        string provider,
        string? providerTransactionId,
        decimal amount,
        string currency,
        DateTimeOffset processedAt)
    {
        var normalizedOrderId = RequireValue(orderId, TransactionRules.OrderIdMaximumLength, nameof(orderId));
        var normalizedReference = RequireValue(reference, TransactionRules.ReferenceMaximumLength, nameof(reference));
        var normalizedProvider = RequireValue(provider, TransactionRules.ProviderMaximumLength, nameof(provider));
        var normalizedProviderTransactionId = NormalizeOptionalValue(
            providerTransactionId,
            TransactionRules.ProviderTransactionIdMaximumLength,
            nameof(providerTransactionId));
        var normalizedCurrency = RequireValue(currency, TransactionRules.CurrencyLength, nameof(currency)).ToUpperInvariant();

        if (!TransactionRules.HasPositiveAmount(amount))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Transaction amount must be greater than zero.");
        }

        if (!TransactionRules.HasValidCurrencyLength(normalizedCurrency))
        {
            throw new ArgumentException("Transaction currency must be a three-letter ISO code.", nameof(currency));
        }

        var now = DateTimeOffset.UtcNow;
        return new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            OrderId = normalizedOrderId,
            UserId = NormalizeOptionalValue(userId, TransactionRules.UserIdMaximumLength, nameof(userId)),
            Reference = normalizedReference,
            TransactionType = TransactionTypes.Payment,
            Status = TransactionStatuses.Successful,
            Provider = normalizedProvider,
            ProviderTransactionId = normalizedProviderTransactionId,
            Amount = amount,
            Currency = normalizedCurrency,
            ProcessedAt = processedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string RequireValue(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalValue(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return RequireValue(value, maximumLength, parameterName);
    }
}
