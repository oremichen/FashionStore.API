using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Abstractions.Images;
using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;

namespace FashionStore.API.Features.Products.GetProductBySlug;
public class GetProductBySlugService(IProductRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<GetProductBySlugService> logger) : IGetProductBySlugService
{
    private static readonly (int Width, int Height)[] ProductImageSizes = [(240, 300), (600, 750), (1200, 1500)];
    private static readonly string[] Statuses = ["draft", "active", "inactive", "archived"];
    private static readonly string[] StockStatuses = ["in-stock", "low-stock", "out-of-stock"];
    private static readonly string[] Sorts = ["newest", "oldest", "name-asc", "name-desc", "price-asc", "price-desc", "stock-asc", "stock-desc"];
    private static readonly string[] StorefrontSorts = ["newest", "popular", "rating", "price-asc", "price-desc"];
    public async Task<ResponseResult<ProductDetailResponse>> ExecuteAsync(string slug, CancellationToken ct)
    {
        var product = await repository.GetBySlugAsync(slug, ct);
        return product is null ? new ResponseResult<ProductDetailResponse>().Fail("Product was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : new ResponseResult<ProductDetailResponse>().Success(MapDetail(product, 5), "Product retrieved successfully.");
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

    private static ProductDetailResponse MapDetail(Product product, int threshold)
    {
        var response = Map(product, threshold);
        return new ProductDetailResponse
        {
            Id = response.Id,
            CategoryId = response.CategoryId,
            CategoryName = response.CategoryName,
            BrandId = response.BrandId,
            BrandName = response.BrandName,
            Name = response.Name,
            Slug = response.Slug,
            Description = response.Description,
            AdditionalInformation = response.AdditionalInformation,
            ShortDescription = response.ShortDescription,
            OldPrice = response.OldPrice,
            NewPrice = response.NewPrice,
            MinPrice = response.MinPrice,
            MaxPrice = response.MaxPrice,
            HasPriceRange = response.HasPriceRange,
            Discount = response.Discount,
            CurrencyCode = response.CurrencyCode,
            AvailabilityCount = response.AvailabilityCount,
            ColorCount = response.ColorCount,
            StockStatus = response.StockStatus,
            Weight = response.Weight,
            WeightUnit = response.WeightUnit,
            IsFeatured = response.IsFeatured,
            IsNewArrival = response.IsNewArrival,
            Status = response.Status,
            PublishedAt = response.PublishedAt,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt,
            Star = response.Star,
            Ratings = response.Ratings,
            Images = response.Images,
            Sizes = response.Sizes,
            Colors = response.Colors,
            ProductVariants = product.Variants.Select(MapVariant).ToList()
        };
    }

    private static IReadOnlyList<ColorResponse> MapColors(Product product) => product.ProductColors.OrderBy(item => item.Color.SortOrder).Select(item => new ColorResponse { Id = item.Color.Id, Name = item.Color.Name, HexCode = item.Color.HexCode, SortOrder = item.Color.SortOrder, IsActive = item.Color.IsActive }).ToList();
    private static IReadOnlyList<SizeResponse> MapSizes(Product product) => product.ProductSizes.OrderBy(item => item.Size.SortOrder).Select(item => new SizeResponse { Id = item.Size.Id, Name = item.Size.Name, DisplayName = item.Size.DisplayName, SortOrder = item.Size.SortOrder, IsActive = item.Size.IsActive }).ToList();

    private static ProductVariantResponse MapVariant(ProductVariant item)
    {
        return new ProductVariantResponse
        {
            Id = item.Id,
            SizeId = item.SizeId,
            Size = item.Size?.DisplayName,
            Price = item.NewPrice,
            Quantity = item.AvailabilityCount
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
