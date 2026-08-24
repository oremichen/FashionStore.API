using FashionStore.Domain.Abstractions.Brands;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.Brands.CreateBrand;
public sealed class CreateBrandService(IBrandRepository repository, ICloudinaryImageService cloudinary, ILogger<CreateBrandService> logger) : ICreateBrandService
{
    public async Task<ResponseResult<BrandResponse>> ExecuteAsync(CreateBrandRequest request, CancellationToken cancellationToken)
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

    private static BrandResponse Map(Brand brand)
    {
        var hasImage = !string.IsNullOrWhiteSpace(brand.ImageUrl);
        return new BrandResponse
        {
            Id = brand.Id,
            Name = brand.Name,
            Slug = brand.Slug,
            Description = brand.Description,
            WebsiteUrl = brand.WebsiteUrl,
            IsActive = brand.IsActive,
            HasImage = hasImage,
            ImageUrl = brand.ImageUrl,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt
        };
    }
}
