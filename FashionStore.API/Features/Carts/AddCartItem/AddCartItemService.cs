using FashionStore.API.Features.Carts.Shared;
using FashionStore.Domain.Abstractions.Carts;
using FashionStore.Domain.Abstractions.Products;

namespace FashionStore.API.Features.Carts.AddCartItem;

public sealed class AddCartItemService(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    ILogger<AddCartItemService> logger) : IAddCartItemService
{
    public async Task<ResponseResult<CartResponse>> ExecuteAsync(string userId, AddCartItemRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding product {ProductId} to cart for user {UserId}.", request.ProductId, userId);
        var product = await productRepository.GetByIdAsync(request.ProductId, false, cancellationToken);
        if (!IsValidSelection(product, request.VariantId, request.ColorId))
        {
            logger.LogWarning("User {UserId} attempted to add an invalid cart selection for product {ProductId}.", userId, request.ProductId);
            return new ResponseResult<CartResponse>().Fail("The selected product options are unavailable.", ResponseCodes.INVALID_ACTION);
        }

        try
        {
            var cart = await cartRepository.AddOrIncreaseItemAsync(
                userId,
                request.ProductId,
                request.VariantId,
                request.ColorId,
                request.Quantity,
                cancellationToken);
            logger.LogInformation("Added product {ProductId} to cart {CartId} for user {UserId}.", request.ProductId, cart.Id, userId);
            return new ResponseResult<CartResponse>().Success(CartResponseMapper.Map(cart), "Item added to cart successfully.");
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "Cart item validation failed for user {UserId}, product {ProductId}.", userId, request.ProductId);
            return new ResponseResult<CartResponse>().Fail("The cart item is invalid.", ResponseCodes.INVALID_ACTION);
        }
        catch (OverflowException exception)
        {
            logger.LogWarning(exception, "Cart quantity overflow for user {UserId}, product {ProductId}.", userId, request.ProductId);
            return new ResponseResult<CartResponse>().Fail("The requested quantity is too large.", ResponseCodes.INVALID_ACTION);
        }
    }

    private static bool IsValidSelection(Product? product, string? variantId, string? colorId)
    {
        if (product is null || !product.IsActive || product.IsArchived)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(variantId))
        {
            if (product.Variants.Count > 0)
            {
                return false;
            }
        }
        else if (!product.Variants.Any(variant => variant.Id == variantId && variant.IsActive))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(colorId))
        {
            return true;
        }

        return product.ProductColors.Any(color => color.ColorId == colorId)
            || product.Variants.Any(variant => variant.ColorId == colorId);
    }
}
