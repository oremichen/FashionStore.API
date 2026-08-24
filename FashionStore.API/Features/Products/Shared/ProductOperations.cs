using FashionStore.Domain.Repositories.Products;
using FashionStore.Infrastructure.Contracts.Abstractions.Images;
using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;

namespace FashionStore.API.Features.Products;

public sealed class ProductOperations(IProductRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<ProductOperations> logger)
{
    private static readonly (int Width, int Height)[] ProductImageSizes = [(240, 300), (600, 750), (1200, 1500)];
    private static readonly string[] Statuses = ["draft", "active", "inactive", "archived"];
    private static readonly string[] StockStatuses = ["in-stock", "low-stock", "out-of-stock"];
    private static readonly string[] Sorts = ["newest", "oldest", "name-asc", "name-desc", "price-asc", "price-desc", "stock-asc", "stock-desc"];
    private static readonly string[] StorefrontSorts = ["newest", "popular", "rating", "price-asc", "price-desc"];

    public async Task<ResponseResult<PagedResponse<ProductResponse>>> GetStorefrontAsync(StorefrontProductQuery query, CancellationToken ct)
    {
        var validation = ValidateStorefrontQuery(query);
        if (validation is not null) return new ResponseResult<PagedResponse<ProductResponse>>().Fail(validation, ResponseCodes.INVALID_ACTION);
        return await GetStorefrontPageAsync(query, null, null, ct);
    }

    public Task<ResponseResult<PagedResponse<ProductResponse>>> GetCollectionAsync(string collection, int page, int pageSize, CancellationToken ct)
    {
        var query = new StorefrontProductQuery { Page = page, PageSize = pageSize };
        if (page < 1 || pageSize is < 1 or > 100)
            return Task.FromResult(new ResponseResult<PagedResponse<ProductResponse>>().Fail("Page and pageSize are invalid.", ResponseCodes.INVALID_ACTION));
        return GetStorefrontPageAsync(query, collection, null, ct);
    }

    public async Task<ResponseResult<ProductDetailResponse>> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var product = await repository.GetBySlugAsync(slug, ct);
        return product is null
            ? new ResponseResult<ProductDetailResponse>().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD)
            : new ResponseResult<ProductDetailResponse>().Success(MapDetail(product, 5), "Product retrieved successfully.");
    }

    public async Task<ResponseResult<PagedResponse<ProductResponse>>> GetRelatedAsync(string productId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return new ResponseResult<PagedResponse<ProductResponse>>().Fail("Page and pageSize are invalid.", ResponseCodes.INVALID_ACTION);
        var product = await repository.GetByIdAsync(productId, false, ct);
        if (product is null || product.IsArchived || !product.IsActive || product.PublishedAt is null)
            return new ResponseResult<PagedResponse<ProductResponse>>().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        var query = new StorefrontProductQuery { CategorySlug = product.CategoryId, Page = page, PageSize = pageSize };
        return await GetStorefrontPageAsync(query, "related", product.Id, ct);
    }

    private async Task<ResponseResult<PagedResponse<ProductResponse>>> GetStorefrontPageAsync(
        StorefrontProductQuery query, string? collection, string? excludingProductId, CancellationToken ct)
    {
        var (items, total) = await repository.GetStorefrontAsync(query, collection, excludingProductId, ct);
        var mapped = items.Select(x => Map(x, 5)).ToList();
        return new ResponseResult<PagedResponse<ProductResponse>>().Success(
            new PagedResponse<ProductResponse> 
            {   Items = mapped, 
                Page = query.Page, 
                PageSize = query.PageSize, 
                TotalCount = total,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize) 
            }, "Products retrieved successfully.");
    }

    private static string? ValidateStorefrontQuery(StorefrontProductQuery query)
    {
        if (!StorefrontSorts.Contains(query.Sort.ToLowerInvariant())) return "The sort value is invalid.";
        if (query.MinPrice < 0 || query.MaxPrice < 0 || query.MinPrice > query.MaxPrice) return "The price range is invalid.";
        return null;
    }

    public async Task<ResponseResult<PagedResponse<ProductResponse>>> GetAsync(ProductQuery query, CancellationToken ct)
    {
        logger.LogInformation("Retrieving products. Page: {Page}, PageSize: {PageSize}, Status: {Status}, StockStatus: {StockStatus}.",
            query.Page, query.PageSize, query.Status, query.StockStatus);
        var response = new ResponseResult<PagedResponse<ProductResponse>>();
        if (!ValidOptional(query.Status, Statuses) || !ValidOptional(query.StockStatus, StockStatuses) || !Sorts.Contains(query.Sort.ToLowerInvariant()))
        {
            logger.LogError("Product retrieval rejected because one or more filter values are invalid.");
            return response.Fail("One or more filter values are invalid.", ResponseCodes.INVALID_ACTION);
        }
        if (query.MinPrice < 0 || query.MaxPrice < 0 || query.MinPrice > query.MaxPrice)
        {
            logger.LogError("Product retrieval rejected because the price range is invalid. MinPrice: {MinPrice}, MaxPrice: {MaxPrice}.", query.MinPrice, query.MaxPrice);
            return response.Fail("The price range is invalid.", ResponseCodes.INVALID_ACTION);
        }
        var (items, total) = await repository.GetAsync(query, ct);
        var mapped = items.Select(x => Map(x, query.LowStockThreshold)).ToList();
        logger.LogInformation("Retrieved {ProductCount} products from {TotalCount} matching products.", items.Count, total);
        return response.Success(new PagedResponse<ProductResponse> { Items = mapped, Page = query.Page, PageSize = query.PageSize,
            TotalCount = total, TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize) }, "Products retrieved successfully.");
    }

    public async Task<ResponseResult<ProductDetailResponse>> GetByIdAsync(string id, CancellationToken ct)
    {
        logger.LogInformation("Retrieving product {ProductId}.", id);
        var product = await repository.GetByIdAsync(id, false, ct);
        if (product is null)
        {
            logger.LogWarning("Product {ProductId} was not found.", id);
            return new ResponseResult<ProductDetailResponse>().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        logger.LogInformation("Retrieved product {ProductId}.", id);
        return new ResponseResult<ProductDetailResponse>().Success(MapDetail(product, 5), "Product retrieved successfully.");
    }

    public async Task<ResponseResult<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating product with slug {ProductSlug}.", request.Slug);
        var validation = await ValidateReferencesAsync(request.CategoryId, request.BrandId, request.Slug, null, ct);
        if (validation is not null)
        {
            logger.LogError("Product creation validation failed for slug {ProductSlug}: {ValidationMessage}.", request.Slug, validation.Value.Message);
            return new ResponseResult<ProductResponse>().Fail(validation.Value.Message, validation.Value.Code);
        }
        var sizeIds = SplitIds(request.Sizes);
        var colorIds = SplitIds(request.Colors);
        var optionValidation = await ValidateOptionsAsync(sizeIds, colorIds, ct);
        if (optionValidation is not null)
        {
            return new ResponseResult<ProductResponse>().Fail(optionValidation, ResponseCodes.INVALID_REFERENCE_PROVIDED);
        }
        try
        {
            var product = Product.Create(request.CategoryId, request.BrandId, request.Name, request.Slug,
                request.NewPrice, request.CurrencyCode, request.AvailabilityCount);
            product.Update(request.CategoryId, request.BrandId, request.Name, request.Slug, request.Description, request.AdditionalInformation,
                request.ShortDescription, request.OldPrice, request.NewPrice, request.CurrencyCode, request.AvailabilityCount,
                request.Weight, request.WeightUnit, request.IsFeatured, request.IsNewArrival);
            product.SetStatus(request.Status);
            product.AddImages(await ProcessImagesAsync(request.ImageRequests, ct));
            await repository.AddAsync(product, ct);
            await repository.SetSizesAndColorsAsync(product.Id, sizeIds, colorIds, ct);
            var saved = await repository.GetByIdAsync(product.Id, false, ct) ?? product;
            logger.LogInformation("Created product {ProductId}.", product.Id);
            return new ResponseResult<ProductResponse>().Success(Map(saved, 5), "Product created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Product creation failed validation for slug {ProductSlug}.", request.Slug);
            return new ResponseResult<ProductResponse>().Fail(ex.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    public async Task<ResponseResult<ProductResponse>> UpdateAsync(UpdateProductRequest request, CancellationToken ct)
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
        var sizeIds = SplitIds(request.Sizes);
        var colorIds = SplitIds(request.Colors);
        var optionValidation = await ValidateOptionsAsync(sizeIds, colorIds, ct);
        if (optionValidation is not null)
        {
            return new ResponseResult<ProductResponse>().Fail(optionValidation, ResponseCodes.INVALID_REFERENCE_PROVIDED);
        }
        try
        {
            product.Update(request.CategoryId, request.BrandId, request.Name, request.Slug, request.Description, request.AdditionalInformation,
                request.ShortDescription, request.OldPrice, request.NewPrice, request.CurrencyCode, request.AvailabilityCount,
                request.Weight, request.WeightUnit, request.IsFeatured, request.IsNewArrival);
            product.SetStatus(request.Status);
            product.AddImages(await ProcessImagesAsync(request.ImageRequests, ct));
            await repository.SaveChangesAsync(ct);
            await repository.SetSizesAndColorsAsync(product.Id, sizeIds, colorIds, ct);
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

    public async Task<ResponseResult> DeleteAsync(string id, CancellationToken ct)
    {
        logger.LogInformation("Deleting product {ProductId}.", id);
        var product = await repository.GetByIdAsync(id, true, ct);
        if (product is null)
        {
            logger.LogWarning("Product {ProductId} was not found for deletion.", id);
            return new ResponseResult().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }
        await repository.DeleteAsync(product, ct);
        foreach (var image in product.Images)
        {
            await cloudinary.DeleteAsync(image.SmallUrl, ct);
            await cloudinary.DeleteAsync(image.MediumUrl, ct);
            await cloudinary.DeleteAsync(image.BigUrl, ct);
        }
        logger.LogInformation("Deleted product {ProductId}.", id);
        return new ResponseResult().Success("Product deleted successfully.");
    }

    public async Task<ResponseResult<IReadOnlyList<ProductImageResponse>>> GetImagesAsync(string productId, CancellationToken ct)
    {
        logger.LogInformation("Retrieving images for product {ProductId}.", productId);
        var product = await repository.GetByIdAsync(productId, false, ct);
        if (product is null)
        {
            logger.LogWarning("Product {ProductId} was not found while retrieving images.", productId);
            return new ResponseResult<IReadOnlyList<ProductImageResponse>>().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var images = product.Images.OrderBy(x => x.SortOrder).Select(MapImage).ToList();
        logger.LogInformation("Retrieved {ImageCount} images for product {ProductId}.", images.Count, productId);
        return new ResponseResult<IReadOnlyList<ProductImageResponse>>().Success(images, "Product images retrieved successfully.");
    }

    public async Task<ResponseResult> DeleteImageAsync(string productId, string imageId, CancellationToken ct)
    {
        logger.LogInformation("Deleting image {ImageId} from product {ProductId}.", imageId, productId);
        var image = await repository.GetImageAsync(productId, imageId, ct);
        if (image is null)
        {
            logger.LogWarning("Image {ImageId} was not found for product {ProductId}.", imageId, productId);
            return new ResponseResult().Fail("Product image was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        if (await repository.GetImageCountAsync(productId, ct) <= 1)
        {
            logger.LogWarning("Image {ImageId} cannot be deleted because it is the only image for product {ProductId}.", imageId, productId);
            return new ResponseResult().Fail(
                "A product must have at least one image. Delete the product instead if you want to remove its only image.",
                ResponseCodes.INVALID_ACTION);
        }

        await repository.DeleteImageAsync(image, ct);
        await cloudinary.DeleteAsync(image.SmallUrl, ct);
        await cloudinary.DeleteAsync(image.MediumUrl, ct);
        await cloudinary.DeleteAsync(image.BigUrl, ct);
        logger.LogInformation("Deleted image {ImageId} from product {ProductId}.", imageId, productId);
        return new ResponseResult().Success("Product image deleted successfully.");
    }

    private async Task<(string Message, string Code)?> ValidateReferencesAsync(string categoryId, string? brandId, string slug, string? id, CancellationToken ct)
    {
        if (!await repository.CategoryExistsAsync(categoryId, ct)) return ("The selected category does not exist.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        if (!string.IsNullOrWhiteSpace(brandId) && !await repository.BrandExistsAsync(brandId, ct)) return ("The selected brand does not exist.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        if (await repository.SlugExistsAsync(slug, id, ct)) return ("A product with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        return null;
    }

    private async Task<string?> ValidateOptionsAsync(
        IReadOnlyCollection<string> sizeIds,
        IReadOnlyCollection<string> colorIds,
        CancellationToken ct)
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
            return [];
        }
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool ValidOptional(string? value, string[] allowed)
    {
        return string.IsNullOrWhiteSpace(value) || allowed.Contains(value.ToLowerInvariant());
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
            Id = image.Id, SmallUrl = image.SmallUrl, MediumUrl = image.MediumUrl, BigUrl = image.BigUrl,
            AlternativeText = image.AlternativeText, SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimary, CreatedAt = image.CreatedAt
        };
    }

    private async Task<IReadOnlyList<(string SmallUrl, string MediumUrl, string BigUrl, string ContentType, string FileName)>> ProcessImagesAsync(
        IReadOnlyList<ProductImageRequest> images, CancellationToken ct)
    {
        var output = new List<(string, string, string, string, string)>(images.Count);
        foreach (var image in images)
        {
            var variants = new ProcessedImage[ProductImageSizes.Length];
            for (var index = 0; index < ProductImageSizes.Length; index++)
            {
                var size = ProductImageSizes[index];
                variants[index] = await imageProcessor.CropAndResizeAsync(
                    image.Data, image.ContentType, image.FileName, size.Width, size.Height, allowUpscale: false, ct);
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

    private static ProductDetailResponse MapDetail(Product product, int threshold)
    {
        var response = Map(product, threshold);
        return new ProductDetailResponse
        {
            Id = response.Id, CategoryId = response.CategoryId, CategoryName = response.CategoryName,
            BrandId = response.BrandId, BrandName = response.BrandName, Name = response.Name, Slug = response.Slug,
            Description = response.Description, AdditionalInformation = response.AdditionalInformation,
            ShortDescription = response.ShortDescription, OldPrice = response.OldPrice, NewPrice = response.NewPrice,
            Discount = response.Discount, CurrencyCode = response.CurrencyCode, AvailabilityCount = response.AvailabilityCount,
            StockStatus = response.StockStatus, Weight = response.Weight, WeightUnit = response.WeightUnit,
            IsFeatured = response.IsFeatured, IsNewArrival = response.IsNewArrival, Status = response.Status,
            PublishedAt = response.PublishedAt, CreatedAt = response.CreatedAt, UpdatedAt = response.UpdatedAt,
            Star = response.Star, Ratings = response.Ratings, Images = response.Images,
            Sizes = product.ProductSizes.OrderBy(item => item.Size.SortOrder).Select(item => new SizeResponse
            {
                Id = item.Size.Id, Name = item.Size.Name, DisplayName = item.Size.DisplayName,
                SortOrder = item.Size.SortOrder, IsActive = item.Size.IsActive
            }).ToList(),
            Colors = product.ProductColors.OrderBy(item => item.Color.SortOrder).Select(item => new ColorResponse
            {
                Id = item.Color.Id, Name = item.Color.Name, HexCode = item.Color.HexCode,
                SortOrder = item.Color.SortOrder, IsActive = item.Color.IsActive
            }).ToList()
        };
    }

    private static int Star(Product product) => product.RatingsCount == 0
        ? 0
        : Math.Clamp((int)Math.Round(product.RatingsValue, MidpointRounding.AwayFromZero), 1, 5);

    private static string? Ratings(Product product) => product.RatingsCount == 0
        ? null
        : product.RatingsCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
