using FashionStore.Domain.Abstractions.Brands;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.Brands.GetBrands;
public sealed class GetBrandsService(IBrandRepository repository, ICloudinaryImageService cloudinary, ILogger<GetBrandsService> logger) : IGetBrandsService
{
    public async Task<ResponseResult<IReadOnlyList<BrandResponse>>> ExecuteAsync(bool availableOnly, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving brands.");
        var brands = await repository.GetAllAsync(availableOnly, cancellationToken);
        var mappedBrands = brands.Select(Map).ToList();
        return new ResponseResult<IReadOnlyList<BrandResponse>>().Success(mappedBrands, "Brands retrieved successfully.");
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
            UpdatedAt = brand.UpdatedAt,
            ProductCount = brand.Products.Count(product => !product.IsArchived && product.IsActive && product.PublishedAt != null)
        };
    }
}
