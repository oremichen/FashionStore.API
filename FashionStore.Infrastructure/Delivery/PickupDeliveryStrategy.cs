using System.Text.Encodings.Web;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Constants;
using FashionStore.Domain.Enums;

namespace FashionStore.Infrastructure.Delivery;

public sealed class PickupDeliveryStrategy : IDeliveryMethodStrategy
{
    private readonly IDeliveryMethodClassifier _classifier;

    public PickupDeliveryStrategy(IDeliveryMethodClassifier classifier)
    {
        _classifier = classifier;
    }

    public string MethodDiscriminator => "pickup";

    public string DeliveryMethodDisplayName => "Pickup";

    public bool ShowAddressInEmails => false;

    public bool ShowDeliveryWindowInEmails => false;

    public bool RequiresStoreAddressValidation => true;

    public string GetStatusLabel(string orderStatus)
    {
        if (orderStatus.Equals(OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase))
        {
            return "Ready for Pickup";
        }

        if (orderStatus.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return "Picked Up";
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
            return "Your order is ready for pickup. Please visit our store during business hours to collect your items.";
        }

        if (orderStatus.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return "Your order has been picked up. Thank you for shopping with us and we hope you enjoy your purchase.";
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
            return "Action recorded: the pickup order has been marked as ready for collection. Ensure items are packaged and the customer has been notified to visit the store.";
        }

        if (orderStatus.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return "Action recorded: the pickup order has been marked as collected. Confirm customer identification was verified and the items were handed over successfully.";
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
            ? EmailNotificationTypeEnum.OrderInternalPickupNotification
            : EmailNotificationTypeEnum.OrderCustomerPickupConfirmation;
    }

    public string GetDeliveryMethodDisplay(string? configuredMethodName, int? estimatedDaysMin, int? estimatedDaysMax)
    {
        return HtmlEncoder.Default.Encode(DeliveryMethodDisplayName);
    }

    public string GetDefaultDeliveryWindowDisplay(int? estimatedDaysMin, int? estimatedDaysMax)
    {
        return HtmlEncoder.Default.Encode("To be confirmed");
    }
}
