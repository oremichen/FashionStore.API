using FashionStore.Domain.Abstractions.Products;

namespace FashionStore.API.Features.Products.GetProductVarient;

public sealed class GetProductVarientService(
    IProductRepository productRepository,
    ILogger<GetProductVarientService> logger) : IGetProductVarientService
{
    public async Task<ResponseResult<IReadOnlyList<ProductVariantResponse>>> ExecuteAsync(
        string productId,
        CancellationToken cancellationToken)
    {
        if (await productRepository.GetByIdAsync(productId, false, cancellationToken) is null)
        {
            logger.LogWarning("Product {ProductId} was not found while retrieving variants.", productId);
            return new ResponseResult<IReadOnlyList<ProductVariantResponse>>()
                .Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var variants = (await productRepository.GetVariantsAsync(productId, cancellationToken))
            .Select(item => new ProductVariantResponse
            {
                Id = item.Id,
                SizeId = item.SizeId,
                Size = item.Size?.DisplayName,
                ColorId = item.ColorId,
                Color = item.Color?.Name,
                Price = item.NewPrice,
                Quantity = item.AvailabilityCount
            })
            .ToList();

        logger.LogInformation("Retrieved {VariantCount} variants for product {ProductId}.", variants.Count, productId);
        return new ResponseResult<IReadOnlyList<ProductVariantResponse>>()
            .Success(variants, "Product variants retrieved successfully.");
    }
}
