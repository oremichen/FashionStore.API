using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Abstractions.Images;
using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;

namespace FashionStore.API.Features.Products.UpdateProduct;
public class UpdateProductService(IProductRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<UpdateProductService> logger) : IUpdateProductService
{
    private static readonly (int Width, int Height)[] ProductImageSizes = [(240, 300), (600, 750), (1200, 1500)];
    private static readonly string[] Statuses = ["draft", "active", "inactive", "archived"];
    private static readonly string[] StockStatuses = ["in-stock", "low-stock", "out-of-stock"];
    private static readonly string[] Sorts = ["newest", "oldest", "name-asc", "name-desc", "price-asc", "price-desc", "stock-asc", "stock-desc"];
    private static readonly string[] StorefrontSorts = ["newest", "popular", "rating", "price-asc", "price-desc"];

    public async Task<ResponseResult<ProductResponse>> ExecuteAsync(UpdateProductRequest request, CancellationToken ct)
    {
        logger.LogInformation("Updating product {ProductId}.", request.ProductId);
        var product = await repository.GetByIdAsync(request.ProductId, true, ct);
        if (product is null)
        {
            logger.LogWarning("Product {ProductId} was not found for update.", request.ProductId);
            return new ResponseResult<ProductResponse>().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var validation = await ValidateReferencesAsync(request.CategoryId, request.BrandId, request.Slug, request.ProductId, ct);
        if (validation is not null)
        {
            logger.LogError("Product {ProductId} update validation failed: {ValidationMessage}.", request.ProductId, validation.Value.Message);
            return new ResponseResult<ProductResponse>().Fail(validation.Value.Message, validation.Value.Code);
        }

        var sizeIds = SplitIds(request.Sizes).Concat(request.ProductVariants.Where(x => !string.IsNullOrWhiteSpace(x.SizeId)).Select(x => x.SizeId!)).Distinct().ToArray();
        var colorIds = SplitIds(request.Colors).Concat(request.ProductVariants.Where(x => !string.IsNullOrWhiteSpace(x.Color)).Select(x => x.Color!)).Distinct().ToArray();
        var optionValidation = await ValidateOptionsAsync(sizeIds, colorIds, ct);
        if (optionValidation is not null)
        {
            return new ResponseResult<ProductResponse>().Fail(optionValidation, ResponseCodes.INVALID_REFERENCE_PROVIDED);
        }
        var priceValidation = ValidatePricing(request);
        if (priceValidation is not null) return new ResponseResult<ProductResponse>().Fail(priceValidation, ResponseCodes.INVALID_ACTION);

        try
        {
            var oldPrice = request.IsMinMaxPrice ? null : request.OldPrice;
            var newPrice = request.IsMinMaxPrice ? 0 : request.NewPrice ?? 0;
            var minPrice = request.IsMinMaxPrice ? request.MinPrice : null;
            var maxPrice = request.IsMinMaxPrice ? request.MaxPrice : null;

            product.Update(request.CategoryId, request.BrandId, request.Name, request.Slug, request.Description, request.AdditionalInformation, request.ShortDescription, oldPrice, newPrice, request.CurrencyCode, request.AvailabilityCount, request.Weight, request.WeightUnit, request.IsFeatured, request.IsNewArrival, request.IsMinMaxPrice, minPrice, maxPrice);
            product.SetStatus(request.Status);
            product.AddImages(await ProcessImagesAsync(request.ImageRequests, ct));
            await repository.SaveChangesAsync(ct);
            await repository.SetSizesAndColorsAsync(product.Id, sizeIds, colorIds, ct);
            await repository.SetVariantsAsync(product.Id, request.ProductVariants.Select(x => (x.Size, x.Color, x.Price, x.Quantity)).ToArray(), ct);
            var saved = await repository.GetByIdAsync(product.Id, false, ct) ?? product;
            logger.LogInformation("Updated product {ProductId}.", product.Id);
            return new ResponseResult<ProductResponse>().Success(Map(saved, 5), "Product updated successfully.");
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Product {ProductId} update failed validation.", request.ProductId);
            return new ResponseResult<ProductResponse>().Fail(ex.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private static string? ValidatePricing(ProductWriteRequest request)
    {
        if (request.IsOldNewPrice && request.IsMinMaxPrice) return "IsOldNewPrice and IsMinMaxPrice cannot both be true.";
        if (request.IsOldNewPrice && !request.NewPrice.HasValue) return "NewPrice is required when IsOldNewPrice is true.";
        if (request.IsOldNewPrice && (request.NewPrice < 0 || request.OldPrice < 0)) return "OldPrice and NewPrice must be non-negative.";
        if (request.IsMinMaxPrice && (!request.MinPrice.HasValue || !request.MaxPrice.HasValue)) return "MinPrice and MaxPrice are required when IsMinMaxPrice is true.";
        if (request.IsMinMaxPrice && (request.MinPrice < 0 || request.MaxPrice < 0 || request.MinPrice > request.MaxPrice)) return "MinPrice and MaxPrice must be non-negative and MinPrice cannot exceed MaxPrice.";
        if (request.ProductVariants.Any(x => x.Price < 0 || x.Quantity < 0 || (string.IsNullOrWhiteSpace(x.Size) && string.IsNullOrWhiteSpace(x.Color)))) return "Each product variant must have a size or color and non-negative price and quantity.";
        if (request.ProductVariants.GroupBy(x => new { x.Size, x.Color }).Any(x => x.Count() > 1)) return "Duplicate product variants are not allowed.";
        return null;
    }

    private async Task<(string Message, string Code)?> ValidateReferencesAsync(string categoryId, string? brandId, string slug, string? id, CancellationToken ct)
    {
        if (!await repository.CategoryExistsAsync(categoryId, ct))
            return ("The selected category does not exist.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        if (!string.IsNullOrWhiteSpace(brandId) && !await repository.BrandExistsAsync(brandId, ct))
            return ("The selected brand does not exist.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        if (await repository.SlugExistsAsync(slug, id, ct))
            return ("A product with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        return null;
    }

    private async Task<string?> ValidateOptionsAsync(IReadOnlyCollection<string> sizeIds, IReadOnlyCollection<string> colorIds, CancellationToken ct)
    {
        if (!await repository.SizeIdsExistAsync(sizeIds, ct))
        {
            return "One or more selected sizes do not exist.";
        }

        if (!await repository.ColorIdsExistAsync(colorIds, ct))
        {
            return "One or more selected colors do not exist.";
        }

        return null;
    }

    private static IReadOnlyCollection<string> SplitIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return[];
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string Status(Product product)
    {
        return product.IsArchived ? "archived" : product.PublishedAt is null ? "draft" : product.IsActive ? "active" : "inactive";
    }

    private static string Stock(Product product, int threshold)
    {
        return product.AvailabilityCount == 0 ? "out-of-stock" : product.AvailabilityCount <= threshold ? "low-stock" : "in-stock";
    }

    private static ProductImageResponse MapImage(ProductImage image)
    {
        return new ProductImageResponse
        {
            Id = image.Id,
            SmallUrl = image.SmallUrl,
            MediumUrl = image.MediumUrl,
            BigUrl = image.BigUrl,
            AlternativeText = image.AlternativeText,
            SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimary,
            CreatedAt = image.CreatedAt
        };
    }

    private async Task<IReadOnlyList<(string SmallUrl, string MediumUrl, string BigUrl, string ContentType, string FileName)>> ProcessImagesAsync(IReadOnlyList<ProductImageRequest> images, CancellationToken ct)
    {
        var output = new List<(string, string, string, string, string)>(images.Count);
        foreach (var image in images)
        {
            var variants = new ProcessedImage[ProductImageSizes.Length];
            for (var index = 0; index < ProductImageSizes.Length; index++)
            {
                var size = ProductImageSizes[index];
                variants[index] = await imageProcessor.CropAndResizeAsync(image.Data, image.ContentType, image.FileName, size.Width, size.Height, allowUpscale: false, ct);
            }

            var small = await cloudinary.UploadWithMetadataAsync(variants[0].Data, variants[0].FileName, ct);
            var medium = await cloudinary.UploadWithMetadataAsync(variants[1].Data, variants[1].FileName, ct);
            var large = await cloudinary.UploadWithMetadataAsync(variants[2].Data, variants[2].FileName, ct);
            output.Add((small.Url, medium.Url, large.Url, large.ContentType, large.FileName));
        }

        return output;
    }

    private static ProductResponse Map(Product product, int threshold)
    {
        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            AdditionalInformation = product.AdditionalInformation,
            ShortDescription = product.ShortDescription,
            OldPrice = product.OldPrice,
            NewPrice = product.NewPrice,
            MinPrice = product.MinPrice,
            MaxPrice = product.MaxPrice,
            HasPriceRange = product.NewPrice == 0 && product.MinPrice.HasValue && product.MaxPrice.HasValue,
            Discount = product.Discount,
            CurrencyCode = product.CurrencyCode,
            AvailabilityCount = product.AvailabilityCount,
            StockStatus = Stock(product, threshold),
            Weight = product.Weight,
            WeightUnit = product.WeightUnit,
            IsFeatured = product.IsFeatured,
            IsNewArrival = product.IsNewArrival,
            Status = Status(product),
            PublishedAt = product.PublishedAt,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Star = Star(product),
            Ratings = Ratings(product),
            Images = product.Images.OrderBy(image => image.SortOrder).Select(MapImage).ToList()
        };
    }

    private static int Star(Product product)
    {
        return product.RatingsCount == 0 ? 0 : Math.Clamp((int)Math.Round(product.RatingsValue, MidpointRounding.AwayFromZero), 1, 5);
    }

    private static string? Ratings(Product product)
    {
        return product.RatingsCount == 0 ? null : product.RatingsCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
