using FashionStore.Domain.Abstractions.Products;
using FashionStore.API.Features.Products.Shared;

namespace FashionStore.API.Features.ProductVariants;
[Route("api/admin/product-variants")]
[ApiController]
[Authorize(Roles = "SuperAdmin,BusinessAdmin")]
public sealed class ProductVariantsController(IProductRepository productRepository) : BaseApiController
{
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProductId(string productId, CancellationToken cancellationToken)
    {
        if (await productRepository.GetByIdAsync(productId, false, cancellationToken) is null)
            return ProcessResponse(new ResponseResult<IReadOnlyList<ProductVariantResponse>>().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD));

        var variants = (await productRepository.GetVariantsAsync(productId, cancellationToken)).Select(item => new ProductVariantResponse
        {
            Id = item.Id, SizeId = item.SizeId, Size = item.Size?.DisplayName,
            ColorId = item.ColorId, Color = item.Color?.Name,
            Price = item.NewPrice, Quantity = item.AvailabilityCount
        }).ToList();
        return ProcessResponse(new ResponseResult<IReadOnlyList<ProductVariantResponse>>().Success(variants, "Product variants retrieved successfully."));
    }
}
