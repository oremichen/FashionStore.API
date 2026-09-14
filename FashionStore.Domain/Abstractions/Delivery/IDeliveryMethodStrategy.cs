using FashionStore.Domain.Enums;

namespace FashionStore.Domain.Abstractions.Delivery;

public interface IDeliveryMethodStrategy
{
    string MethodDiscriminator { get; }

    string DeliveryMethodDisplayName { get; }

    bool ShowAddressInEmails { get; }

    bool ShowDeliveryWindowInEmails { get; }

    bool RequiresStoreAddressValidation { get; }

    string GetStatusLabel(string orderStatus);

    string GetCustomerStatusMessage(string orderStatus);

    string GetAdminStatusMessage(string orderStatus);

    EmailNotificationTypeEnum GetOrderConfirmationTemplate(bool isInternal);

    string GetDeliveryMethodDisplay(string? configuredMethodName, int? estimatedDaysMin, int? estimatedDaysMax);

    string GetDefaultDeliveryWindowDisplay(int? estimatedDaysMin, int? estimatedDaysMax);
}
