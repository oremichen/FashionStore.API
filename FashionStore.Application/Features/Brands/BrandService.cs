using FashionStore.Application.Abstractions.Brands;

namespace FashionStore.Application.Features.Brands;

public sealed class BrandService(IBrandRepository repository) : IBrandService
{
    public async Task<ResponseResult<BrandResponse>> CreateAsync(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<BrandResponse>();
        if (await repository.NameOrSlugExistsAsync(request.Name.Trim(), request.Slug.Trim(), cancellationToken))
            return response.Fail("A brand with this name or slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        try
        {
            var brand = Brand.Create(request.Name, request.Slug, request.Description, request.WebsiteUrl, request.IsActive);
            if (request.ImageData is { Length: > 0 }) brand.SetImage(request.ImageData, request.ImageContentType ?? string.Empty, request.ImageFileName ?? string.Empty);
            await repository.AddAsync(brand, cancellationToken);
            return response.Success(Map(brand), "Brand created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException ex) { return response.Fail(ex.Message, ResponseCodes.INVALID_ACTION); }
    }

    public async Task<ResponseResult<IReadOnlyList<BrandResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var brands = await repository.GetAllAsync(cancellationToken);
        var mappedBrands = brands.Select(Map).ToList();
        return new ResponseResult<IReadOnlyList<BrandResponse>>()
            .Success(mappedBrands, "Brands retrieved successfully.");
    }
    
    public async Task<BrandImageResponse?> GetImageAsync(string id, CancellationToken cancellationToken)
    {
        var brand = await repository.GetByIdAsync(id.Trim(), cancellationToken);
        return brand?.ImageData is null ? null : new(brand.ImageData, brand.ImageContentType!, brand.ImageFileName!);
    }

    private static BrandResponse Map(Brand brand)
    {
        var hasImage = brand.ImageData is { Length: > 0 };
        var imageUrl = hasImage ? $"/api/brands/{brand.Id}/image" : null;

        return new BrandResponse(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.Description,
            brand.WebsiteUrl,
            brand.IsActive,
            hasImage,
            imageUrl,
            brand.CreatedAt,
            brand.UpdatedAt);
    }
}
