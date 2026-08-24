using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Abstractions.Images;
using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;

namespace FashionStore.API.Features.Products.GetProductImages;
public class GetProductImagesService(IProductRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<GetProductImagesService> logger) : IGetProductImagesService
{
    private static readonly (int Width, int Height)[] ProductImageSizes = [(240, 300), (600, 750), (1200, 1500)];
    private static readonly string[] Statuses = ["draft", "active", "inactive", "archived"];
    private static readonly string[] StockStatuses = ["in-stock", "low-stock", "out-of-stock"];
    private static readonly string[] Sorts = ["newest", "oldest", "name-asc", "name-desc", "price-asc", "price-desc", "stock-asc", "stock-desc"];
    private static readonly string[] StorefrontSorts = ["newest", "popular", "rating", "price-asc", "price-desc"];

    public async Task<ResponseResult<IReadOnlyList<ProductImageResponse>>> ExecuteAsync(string productId, CancellationToken ct)
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
}
