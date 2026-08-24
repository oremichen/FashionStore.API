using FashionStore.Domain.Abstractions.Brands;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.Brands;

public sealed class BrandOperations(IBrandRepository repository, ICloudinaryImageService cloudinary, ILogger<BrandOperations> logger)
{
    public async Task<ResponseResult<BrandResponse>> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating brand with slug {Slug}.", request.Slug);
        var response = new ResponseResult<BrandResponse>();
        if (await repository.NameOrSlugExistsAsync(request.Name.Trim(), request.Slug.Trim(), cancellationToken))
        {
            logger.LogError("Brand creation validation failed because name {BrandName} or slug {BrandSlug} already exists.", request.Name, request.Slug);
            return response.Fail("A brand with this name or slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        }
        try
        {
            var brand = Brand.Create(request.Name, request.Slug, request.Description, request.WebsiteUrl, request.IsActive);
            if (request.ImageData is { Length: > 0 })
            {
                var upload = await cloudinary.UploadWithMetadataAsync(request.ImageData, request.ImageFileName ?? "brand-image", cancellationToken);
                brand.SetImageUrl(upload.Url, upload.ContentType, upload.FileName);
            }
            await repository.AddAsync(brand, cancellationToken);
            logger.LogInformation("Created brand {BrandId}.", brand.Id);
            return response.Success(Map(brand), "Brand created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Brand creation validation failed for slug {Slug}.", request.Slug);
            return response.Fail(ex.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    public async Task<ResponseResult<IReadOnlyList<BrandResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving brands.");
        var brands = await repository.GetAllAsync(cancellationToken);
        var mappedBrands = brands.Select(Map).ToList();
        return new ResponseResult<IReadOnlyList<BrandResponse>>()
            .Success(mappedBrands, "Brands retrieved successfully.");
    }
    
    public async Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken)
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

    private static BrandResponse Map(Brand brand)
    {
        var hasImage = !string.IsNullOrWhiteSpace(brand.ImageUrl);

        return new BrandResponse
        {
            Id = brand.Id, Name = brand.Name, Slug = brand.Slug, Description = brand.Description,
            WebsiteUrl = brand.WebsiteUrl, IsActive = brand.IsActive, HasImage = hasImage,
            ImageUrl = brand.ImageUrl, CreatedAt = brand.CreatedAt, UpdatedAt = brand.UpdatedAt
        };
    }
}
