using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Abstractions.Images;
using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;

namespace FashionStore.API.Features.Products.GetProducts;
public class GetProductsService(IProductRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<GetProductsService> logger) : IGetProductsService
{
    private static readonly (int Width, int Height)[] ProductImageSizes = [(240, 300), (600, 750), (1200, 1500)];
    private static readonly string[] Statuses = ["draft", "active", "inactive", "archived"];
    private static readonly string[] StockStatuses = ["in-stock", "low-stock", "out-of-stock"];
    private static readonly string[] Sorts = ["newest", "oldest", "name-asc", "name-desc", "price-asc", "price-desc", "stock-asc", "stock-desc"];
    private static readonly string[] StorefrontSorts = ["newest", "popular", "rating", "price-asc", "price-desc"];

    public async Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(ProductQuery query, CancellationToken ct)
    {
        logger.LogInformation("Retrieving products. Page: {Page}, PageSize: {PageSize}, Status: {Status}, StockStatus: {StockStatus}.", query.Page, query.PageSize, query.Status, query.StockStatus);
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

        var(items, total) = await repository.GetAsync(query, ct);
        var mapped = items.Select(x => Map(x, query.LowStockThreshold)).ToList();
        logger.LogInformation("Retrieved {ProductCount} products from {TotalCount} matching products.", items.Count, total);
        return response.Success(new PagedResponse<ProductResponse> { Items = mapped, Page = query.Page, PageSize = query.PageSize, TotalCount = total, TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize) }, "Products retrieved successfully.");
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

    private static ProductResponse Map(Product product, int threshold)
    {
        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            Name = ProductNameFormatter.CapitalizeWords(product.Name),
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
            ColorCount = product.ProductColors.Count,
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
            Images = product.Images.OrderBy(image => image.SortOrder).Select(MapImage).ToList(),
            Colors = MapColors(product),
            Sizes = MapSizes(product)
        };
    }

    private static IReadOnlyList<ColorResponse> MapColors(Product product) => product.ProductColors.OrderBy(item => item.Color.SortOrder).Select(item => new ColorResponse { Id = item.Color.Id, Name = item.Color.Name, HexCode = item.Color.HexCode, SortOrder = item.Color.SortOrder, IsActive = item.Color.IsActive }).ToList();
    private static IReadOnlyList<SizeResponse> MapSizes(Product product) => product.ProductSizes.OrderBy(item => item.Size.SortOrder).Select(item => new SizeResponse { Id = item.Size.Id, Name = item.Size.Name, DisplayName = item.Size.DisplayName, SortOrder = item.Size.SortOrder, IsActive = item.Size.IsActive }).ToList();

    private static int Star(Product product)
    {
        return product.RatingsCount == 0 ? 0 : Math.Clamp((int)Math.Round(product.RatingsValue, MidpointRounding.AwayFromZero), 1, 5);
    }

    private static string? Ratings(Product product)
    {
        return product.RatingsCount == 0 ? null : product.RatingsCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
