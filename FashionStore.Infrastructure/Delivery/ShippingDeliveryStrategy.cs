using System.Text.Encodings.Web;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Constants;
using FashionStore.Domain.Enums;

namespace FashionStore.Infrastructure.Delivery;

public sealed class ShippingDeliveryStrategy : IDeliveryMethodStrategy
{
    private readonly IDeliveryMethodClassifier _classifier;

    public ShippingDeliveryStrategy(IDeliveryMethodClassifier classifier)
    {
        _classifier = classifier;
    }

    public string MethodDiscriminator => "shipping";

    public string DeliveryMethodDisplayName => "Delivery";

    public bool ShowAddressInEmails => true;

    public bool ShowDeliveryWindowInEmails => true;

    public bool RequiresStoreAddressValidation => false;

    public string GetStatusLabel(string orderStatus)
    {
        if (orderStatus.Equals(OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase))
        {
            return "Shipping";
        }

        if (orderStatus.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return "Delivered";
        }

        if (orderStatus.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return "Cancelled";
        }

        return "Returned";
    }

    public string GetCustomerStatusMessage(string orderStatus)
    {
        if (orderStatus.Equals(OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase))
        {
            return "Your order has been shipped and is on its way to you.";
        }

        if (orderStatus.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return "Your order has been delivered. We hope you enjoy your purchase.";
        }

        if (orderStatus.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return "Your order has been cancelled. If a refund applies, it will be processed according to our policy.";
        }

        return "Your returned order has been received and is being reviewed.";
    }

    public string GetAdminStatusMessage(string orderStatus)
    {
        if (orderStatus.Equals(OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase))
        {
            return "Action recorded: the order has been marked as shipped. Confirm courier handover and tracking details are available to the customer.";
        }

        if (orderStatus.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return "Action recorded: the order has been marked as delivered. Confirm delivery completion and resolve any outstanding fulfilment tasks.";
        }

        if (orderStatus.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            return "Action recorded: the order has been cancelled. Review the cancellation reason and process any applicable refund or stock adjustment.";
        }

        return "Action recorded: the order has been marked as returned. Inspect the returned items and update the refund or restocking outcome.";
    }

    public EmailNotificationTypeEnum GetOrderConfirmationTemplate(bool isInternal)
    {
        return isInternal
            ? EmailNotificationTypeEnum.OrderInternalNotification
            : EmailNotificationTypeEnum.OrderCustomerConfirmation;
    }

    public string GetDeliveryMethodDisplay(string? configuredMethodName, int? estimatedDaysMin, int? estimatedDaysMax)
    {
        if (string.IsNullOrWhiteSpace(configuredMethodName))
        {
            return HtmlEncoder.Default.Encode(DeliveryMethodDisplayName);
        }

        return HtmlEncoder.Default.Encode(configuredMethodName);
    }

    public string GetDefaultDeliveryWindowDisplay(int? estimatedDaysMin, int? estimatedDaysMax)
    {
        return _classifier.FormatDeliveryWindow(estimatedDaysMin, estimatedDaysMax);
    }
}
