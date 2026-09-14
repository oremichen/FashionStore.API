using System.Globalization;
using System.Text.Encodings.Web;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Entities;

namespace FashionStore.Infrastructure.Delivery;

public sealed class DeliveryMethodClassifier : IDeliveryMethodClassifier
{
    public bool IsPickup(string? deliveryMethodName)
    {
        return deliveryMethodName?.Contains("pickup", StringComparison.OrdinalIgnoreCase) == true;
    }

    public string FormatDeliveryAddress(Address? address)
    {
        if (address is null)
        {
            return HtmlEncoder.Default.Encode("Address not available");
        }

        var parts = new List<string>();

        AddPart(parts, address.Street);
        AddPart(parts, address.City);
        AddPart(parts, address.State);
        AddPart(parts, address.Country);
        AddPart(parts, address.PostalCode);
        AddPart(parts, address.Landmark);

        var joined = string.Join(", ", parts);

        return HtmlEncoder.Default.Encode(joined);
    }

    public string FormatDeliveryWindow(int? estimatedDaysMin, int? estimatedDaysMax)
    {
        if (!estimatedDaysMin.HasValue && !estimatedDaysMax.HasValue)
        {
            return HtmlEncoder.Default.Encode("Timing to be confirmed");
        }

        if (estimatedDaysMin == estimatedDaysMax)
        {
            return HtmlEncoder.Default.Encode(
                $"{estimatedDaysMin} day" + (estimatedDaysMin == 1 ? string.Empty : "s"));
        }

        if (!estimatedDaysMin.HasValue)
        {
            return HtmlEncoder.Default.Encode($"Up to {estimatedDaysMax} days");
        }

        if (!estimatedDaysMax.HasValue)
        {
            return HtmlEncoder.Default.Encode($"From {estimatedDaysMin} days");
        }

        return HtmlEncoder.Default.Encode($"{estimatedDaysMin}-{estimatedDaysMax} days");
    }

    public string FormatNaira(decimal amount)
    {
        var formatted = amount.ToString("#,##0.00", CultureInfo.GetCultureInfo("en-NG"));
        return HtmlEncoder.Default.Encode($"₦{formatted}");
    }

    private static void AddPart(List<string> parts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(value);
        }
    }
}
