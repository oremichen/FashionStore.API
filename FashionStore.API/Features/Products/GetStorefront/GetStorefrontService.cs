using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Abstractions.Images;
using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;

namespace FashionStore.API.Features.Products.GetStorefront;
public class GetStorefrontService(IProductRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<GetStorefrontService> logger) : IGetStorefrontService
{
    private static readonly (int Width, int Height)[] ProductImageSizes = [(240, 300), (600, 750), (1200, 1500)];
    private static readonly string[] Statuses = ["draft", "active", "inactive", "archived"];
    private static readonly string[] StockStatuses = ["in-stock", "low-stock", "out-of-stock"];
    private static readonly string[] Sorts = ["newest", "oldest", "name-asc", "name-desc", "price-asc", "price-desc", "stock-asc", "stock-desc"];
    private static readonly string[] StorefrontSorts = ["newest", "popular", "rating", "price-asc", "price-desc"];
    public async Task<ResponseResult<PagedResponse<ProductResponse>>> ExecuteAsync(StorefrontProductQuery query, CancellationToken ct)
    {
        var validation = ValidateStorefrontQuery(query);
        if (validation is not null)
            return new ResponseResult<PagedResponse<ProductResponse>>().Fail(validation, ResponseCodes.INVALID_ACTION);
        return await GetStorefrontPageAsync(query, null, null, ct);
    }

    private async Task<ResponseResult<PagedResponse<ProductResponse>>> GetStorefrontPageAsync(StorefrontProductQuery query, string? collection, string? excludingProductId, CancellationToken ct)
    {
        var(items, total) = await repository.GetStorefrontAsync(query, collection, excludingProductId, ct);
        var mapped = items.Select(x => Map(x, 5)).ToList();
        return new ResponseResult<PagedResponse<ProductResponse>>().Success(new PagedResponse<ProductResponse> { Items = mapped, Page = query.Page, PageSize = query.PageSize, TotalCount = total, TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize) }, "Products retrieved successfully.");
    }

    private static string? ValidateStorefrontQuery(StorefrontProductQuery query)
    {
        if (!StorefrontSorts.Contains(query.Sort.ToLowerInvariant()))
            return "The sort value is invalid.";
        if (query.MinPrice < 0 || query.MaxPrice < 0 || query.MinPrice > query.MaxPrice)
            return "The price range is invalid.";
        return null;
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
