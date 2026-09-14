using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Delivery;

public interface IDeliveryMethodClassifier
{
    bool IsPickup(string? deliveryMethodName);

    string FormatDeliveryAddress(Address? address);

    string FormatDeliveryWindow(int? estimatedDaysMin, int? estimatedDaysMax);

    string FormatNaira(decimal amount);
}
