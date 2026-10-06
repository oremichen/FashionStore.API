using FashionStore.Domain.Constants;

namespace FashionStore.Domain.Entities;

internal static class TransactionRules
{
    internal const int IdMaximumLength = 50;
    internal const int OrderIdMaximumLength = 50;
    internal const int UserIdMaximumLength = 450;
    internal const int ReferenceMaximumLength = 150;
    internal const int TransactionTypeMaximumLength = 30;
    internal const int StatusMaximumLength = 30;
    internal const int ProviderMaximumLength = 50;
    internal const int ProviderTransactionIdMaximumLength = 100;
    internal const int CurrencyLength = 3;
    internal const int ChannelMaximumLength = 50;
    internal const int GatewayResponseCodeMaximumLength = 50;
    internal const int GatewayResponseMessageMaximumLength = 500;
    internal const int FailureReasonMaximumLength = 500;

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
