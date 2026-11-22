using FashionStore.Domain.Constants;

namespace FashionStore.Domain.Entities;

public static class TransactionRules
{
    public const int IdMaximumLength = 50;
    public const int OrderIdMaximumLength = 50;
    public const int UserIdMaximumLength = 450;
    public const int ReferenceMaximumLength = 150;
    public const int TransactionTypeMaximumLength = 30;
    public const int StatusMaximumLength = 30;
    public const int ProviderMaximumLength = 50;
    public const int ProviderTransactionIdMaximumLength = 100;
    public const int CurrencyLength = 3;
    public const int ChannelMaximumLength = 50;
    public const int GatewayResponseCodeMaximumLength = 50;
    public const int GatewayResponseMessageMaximumLength = 500;
    public const int FailureReasonMaximumLength = 500;

    internal static bool IsSupportedTransactionType(string value)
    {
        return value is TransactionTypes.Payment or TransactionTypes.Refund or TransactionTypes.Chargeback or TransactionTypes.Adjustment;
    }

    internal static bool IsSupportedStatus(string value)
    {
        return value is TransactionStatuses.Pending or TransactionStatuses.Processing or TransactionStatuses.Successful or TransactionStatuses.Failed or TransactionStatuses.Reversed or TransactionStatuses.Cancelled;
    }

    internal static bool HasPositiveAmount(decimal amount)
    {
        return amount > 0;
    }

    internal static bool HasValidCurrencyLength(string? currency)
    {
        return currency?.Length == CurrencyLength;
    }
}
