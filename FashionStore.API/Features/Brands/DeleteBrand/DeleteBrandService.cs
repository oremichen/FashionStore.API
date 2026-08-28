using FashionStore.Domain.Abstractions.Brands;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.Brands.DeleteBrand;
public sealed class DeleteBrandService(IBrandRepository repository, ICloudinaryImageService cloudinary, ILogger<DeleteBrandService> logger) : IDeleteBrandService
{
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting brand {BrandId}.", id);
        var response = new ResponseResult();
        if (string.IsNullOrWhiteSpace(id))
            return response.Fail("Brand id is required.", ResponseCodes.INVALID_ACTION);
        var brandId = id.Trim();
        var brand = await repository.GetByIdAsync(brandId, cancellationToken);
        if (brand is null)
            return response.Fail("Brand was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (await repository.HasProductsAsync(brandId, cancellationToken))
        {
            logger.LogError("Brand {BrandId} cannot be deleted because it is mapped to a product.", brandId);
            return response.Fail("Brand cannot be deleted because it is already mapped to a product.", ResponseCodes.INVALID_ACTION);
        }

        await repository.DeleteAsync(brand, cancellationToken);
        await cloudinary.DeleteAsync(brand.ImageUrl, cancellationToken);
        logger.LogInformation("Deleted brand {BrandId}.", brandId);
        return response.Success("Brand deleted successfully.");
    }
}
