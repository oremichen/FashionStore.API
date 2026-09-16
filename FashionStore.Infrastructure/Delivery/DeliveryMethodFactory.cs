using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Entities;

namespace FashionStore.Infrastructure.Delivery;

public sealed class DeliveryMethodFactory : IDeliveryMethodFactory
{
    private readonly PickupDeliveryStrategy _pickupStrategy;
    private readonly ShippingDeliveryStrategy _shippingStrategy;

    public DeliveryMethodFactory(
        PickupDeliveryStrategy pickupStrategy,
        ShippingDeliveryStrategy shippingStrategy)
    {
        _pickupStrategy = pickupStrategy;
        _shippingStrategy = shippingStrategy;
    }

    public IDeliveryMethodStrategy Pickup => _pickupStrategy;

    public IDeliveryMethodStrategy DefaultShipping => _shippingStrategy;

    public IDeliveryMethodStrategy GetForMethod(DeliveryMethod? deliveryMethod)
    {
        return GetForMethod(deliveryMethod?.Name);
    }

    public IDeliveryMethodStrategy GetForMethod(string? deliveryMethodName)
    {
        if (string.IsNullOrWhiteSpace(deliveryMethodName))
        {
            return _shippingStrategy;
        }

        if (deliveryMethodName.Contains("pickup", StringComparison.OrdinalIgnoreCase))
        {
            return _pickupStrategy;
        }

        return _shippingStrategy;
    }
}
