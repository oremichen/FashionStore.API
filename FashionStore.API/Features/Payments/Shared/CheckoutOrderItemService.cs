using FashionStore.Domain.Abstractions.CatalogOptions;
using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Entities;

namespace FashionStore.API.Features.Payments.Shared;

public sealed class CheckoutOrderItemService(
    IProductRepository productRepository,
    ICatalogOptionRepository catalogOptionRepository,
    ILogger<CheckoutOrderItemService> logger) : ICheckoutOrderItemService
{
    public async Task<List<OrderItem>?> BuildAsync(IEnumerable<CheckoutItemRequest> requestedItems, string userId, CancellationToken cancellationToken)
    {
        var orderItems = new List<OrderItem>();
        foreach (var requestedItem in requestedItems)
        {
            if (requestedItem.Quantity <= 0)
            {
                return null;
            }

            var product = await productRepository.GetByIdAsync(requestedItem.ProductId, false, cancellationToken);
            if (product is null || !product.IsActive || product.IsArchived)
            {
                return null;
            }

            string? colorName = null;
            string? sizeName = null;
            decimal unitPrice;

            if (!string.IsNullOrWhiteSpace(requestedItem.VariantId))
            {
                var variant = product.Variants.FirstOrDefault(item => item.Id == requestedItem.VariantId && item.IsActive);
                if (variant is null || product.AvailabilityCount < requestedItem.Quantity)
                {
                    return null;
                }

                unitPrice = variant.NewPrice;
                if (!string.IsNullOrWhiteSpace(variant.SizeId))
                {
                    var size = await catalogOptionRepository.GetSizeByIdAsync(variant.SizeId, cancellationToken);
                    sizeName = size?.DisplayName ?? size?.Name;
                }

                if (!string.IsNullOrWhiteSpace(variant.ColorId))
                {
                    var color = await catalogOptionRepository.GetColorByIdAsync(variant.ColorId, cancellationToken);
                    colorName = color?.Name;
                }
            }
            else
            {
                if (product.Variants.Count > 0 || product.AvailabilityCount < requestedItem.Quantity)
                {
                    return null;
                }

                unitPrice = product.NewPrice;
            }

            if (!string.IsNullOrWhiteSpace(requestedItem.ColorId))
            {
                var color = await catalogOptionRepository.GetColorByIdAsync(requestedItem.ColorId, cancellationToken);
                if (color is null)
                {
                    logger.LogWarning("User {UserId} selected invalid color {ColorId} for product {ProductId}.", userId, requestedItem.ColorId, product.Id);
                    return null;
                }

                colorName = color.Name;
            }

            if (!string.Equals(product.CurrencyCode, "NGN", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            orderItems.Add(OrderItem.Create(
                product.Id,
                requestedItem.VariantId,
                requestedItem.ColorId,
                colorName,
                sizeName,
                product.Name,
                unitPrice,
                requestedItem.Quantity));
        }

        return orderItems;
    }
}
