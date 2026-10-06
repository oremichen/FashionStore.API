using FashionStore.Domain.Constants;
using System.Text.Json;

namespace FashionStore.Domain.Entities;

internal static class InvoiceRules
{
    internal const int IdMaximumLength = 50;
    internal const int OrderIdMaximumLength = 50;
    internal const int UserIdMaximumLength = 450;
    internal const int InvoiceNumberMaximumLength = 50;
    internal const int StatusMaximumLength = 30;
    internal const int CustomerNameMaximumLength = 250;
    internal const int CustomerEmailMaximumLength = 320;
    internal const int CurrencyLength = 3;
    internal const int PdfUrlMaximumLength = 1000;

    internal static bool IsSupportedStatus(string value)
    {
        return value is InvoiceStatuses.Draft or InvoiceStatuses.Issued or InvoiceStatuses.Paid or InvoiceStatuses.PartiallyPaid or InvoiceStatuses.Overdue or InvoiceStatuses.Cancelled or InvoiceStatuses.Refunded;
    }

    internal static bool HasNonNegativeAmounts(
        decimal subtotal,
        decimal deliveryFee,
        decimal taxAmount,
        decimal discountAmount,
        decimal totalAmount)
    {
        return subtotal >= 0 &&
            deliveryFee >= 0 &&
            taxAmount >= 0 &&
            discountAmount >= 0 &&
            totalAmount >= 0;
    }

    internal static bool HasValidCurrencyLength(string? currency)
    {
        return currency?.Length == CurrencyLength;
    }

    internal static bool HasLineItemsArray(string? lineItems)
    {
        if (string.IsNullOrWhiteSpace(lineItems))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(lineItems);
            return document.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
