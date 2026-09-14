using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Delivery;

public interface IDeliveryMethodFactory
{
    IDeliveryMethodStrategy GetForMethod(string? deliveryMethodName);

    IDeliveryMethodStrategy GetForMethod(DeliveryMethod? deliveryMethod);

    IDeliveryMethodStrategy DefaultShipping { get; }

    IDeliveryMethodStrategy Pickup { get; }
}
