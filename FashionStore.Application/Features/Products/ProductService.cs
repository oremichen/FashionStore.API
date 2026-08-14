using FashionStore.Application.Abstractions.Products;
using FashionStore.Application.Abstractions.Images;
using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;

namespace FashionStore.Application.Features.Products;

public sealed class ProductService(IProductRepository repository, IImageProcessor imageProcessor, ILogger<ProductService> logger) : IProductService
{
    private static readonly (int Width, int Height)[] ProductImageSizes = [(240, 300), (600, 750), (1200, 1500)];
    private static readonly string[] Statuses = ["draft", "active", "inactive", "archived"];
    private static readonly string[] StockStatuses = ["in-stock", "low-stock", "out-of-stock"];
    private static readonly string[] Sorts = ["newest", "oldest", "name-asc", "name-desc", "price-asc", "price-desc", "stock-asc", "stock-desc"];

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
        return response.Success(new PagedResponse<ProductResponse>(mapped, query.Page, query.PageSize, total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize)), "Products retrieved successfully.");
    }

    public async Task<ResponseResult<ProductResponse>> GetByIdAsync(string id, CancellationToken ct)
    {
        logger.LogInformation("Retrieving product {ProductId}.", id);
        var product = await repository.GetByIdAsync(id, false, ct);
        if (product is null)
        {
            logger.LogWarning("Product {ProductId} was not found.", id);
            return new ResponseResult<ProductResponse>().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        logger.LogInformation("Retrieved product {ProductId}.", id);
        return new ResponseResult<ProductResponse>().Success(Map(product, 5), "Product retrieved successfully.");
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
        try
        {
            var product = Product.Create(request.CategoryId, request.BrandId, request.Name, request.Slug,
                request.NewPrice, request.CurrencyCode, request.AvailabilityCount);
            product.Update(request.CategoryId, request.BrandId, request.Name, request.Slug, request.Description,
                request.ShortDescription, request.OldPrice, request.NewPrice, request.CurrencyCode, request.AvailabilityCount,
                request.Weight, request.WeightUnit, request.IsFeatured, request.IsNewArrival);
            product.SetStatus(request.Status);
            product.AddImages(await ProcessImagesAsync(request.Images, ct));
            await repository.AddAsync(product, ct);
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
        try
        {
            product.Update(request.CategoryId, request.BrandId, request.Name, request.Slug, request.Description,
                request.ShortDescription, request.OldPrice, request.NewPrice, request.CurrencyCode, request.AvailabilityCount,
                request.Weight, request.WeightUnit, request.IsFeatured, request.IsNewArrival);
            product.SetStatus(request.Status);
            product.AddImages(await ProcessImagesAsync(request.Images, ct));
            await repository.SaveChangesAsync(ct);
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
        await repository.DeleteImageAsync(image, ct);
        logger.LogInformation("Deleted image {ImageId} from product {ProductId}.", imageId, productId);
        return new ResponseResult().Success("Product image deleted successfully.");
    }

    public async Task<ProductImageFileResponse?> GetImageAsync(string productId, string imageId, string size, CancellationToken ct)
    {
        logger.LogInformation("Retrieving image {ImageId} for product {ProductId}.", imageId, productId);
        var image = await repository.GetImageAsync(productId, imageId, ct);
        var data = size.ToLowerInvariant() switch
        {
            "small" => image?.SmallImageData,
            "medium" => image?.MediumImageData,
            "large" => image?.ImageData,
            _ => null
        };
        return data is null
            ? null
            : new ProductImageFileResponse(
                data,
                size.Equals("large", StringComparison.OrdinalIgnoreCase) ? image!.ImageContentType! : "image/webp",
                image!.ImageFileName!);
    }

    private async Task<(string Message, string Code)?> ValidateReferencesAsync(string categoryId, string? brandId, string slug, string? id, CancellationToken ct)
    {
        if (!await repository.CategoryExistsAsync(categoryId, ct)) return ("The selected category does not exist.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        if (!string.IsNullOrWhiteSpace(brandId) && !await repository.BrandExistsAsync(brandId, ct)) return ("The selected brand does not exist.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
        if (await repository.SlugExistsAsync(slug, id, ct)) return ("A product with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        return null;
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
        var baseUrl = $"/api/products/{image.ProductId}/images/{image.Id}";
        var smallUrl = image.SmallImageData is { Length: > 0 } ? $"{baseUrl}/small" : image.SmallUrl;
        var mediumUrl = image.MediumImageData is { Length: > 0 } ? $"{baseUrl}/medium" : image.MediumUrl;
        var largeUrl = image.ImageData is { Length: > 0 } ? $"{baseUrl}/large" : image.BigUrl;
        return new ProductImageResponse(image.Id, smallUrl, mediumUrl, largeUrl, image.AlternativeText,
            image.SortOrder, image.IsPrimary, image.CreatedAt);
    }

    private async Task<IReadOnlyList<(byte[] SmallData, byte[] MediumData, byte[] LargeData, string FileName)>> ProcessImagesAsync(
        IReadOnlyList<ProductImageRequest> images, CancellationToken ct)
    {
        var output = new List<(byte[], byte[], byte[], string)>(images.Count);
        foreach (var image in images)
        {
            var variants = new ProcessedImage[ProductImageSizes.Length];
            for (var index = 0; index < ProductImageSizes.Length; index++)
            {
                var size = ProductImageSizes[index];
                variants[index] = await imageProcessor.CropAndResizeAsync(
                    image.Data, image.ContentType, image.FileName, size.Width, size.Height, ct);
            }
            output.Add((variants[0].Data, variants[1].Data, variants[2].Data, variants[2].FileName));
        }
        return output;
    }

    private static ProductResponse Map(Product product, int threshold)
    {
        return new ProductResponse(product.Id, product.CategoryId, product.Category.Name, product.BrandId,
            product.Brand?.Name, product.Name, product.Slug, product.Description, product.ShortDescription, product.OldPrice,
            product.NewPrice, product.Discount, product.CurrencyCode, product.AvailabilityCount, Stock(product, threshold),
            product.Weight, product.WeightUnit, product.IsFeatured, product.IsNewArrival, Status(product), product.PublishedAt,
            product.CreatedAt, product.UpdatedAt, product.Images.OrderBy(image => image.SortOrder).Select(MapImage).ToList());
    }
}
