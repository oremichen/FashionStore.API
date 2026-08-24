using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Abstractions.Images;
using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;

namespace FashionStore.API.Features.Products.DeleteProduct;
public class DeleteProductService(IProductRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<DeleteProductService> logger) : IDeleteProductService
{
    private static readonly (int Width, int Height)[] ProductImageSizes = [(240, 300), (600, 750), (1200, 1500)];
    private static readonly string[] Statuses = ["draft", "active", "inactive", "archived"];
    private static readonly string[] StockStatuses = ["in-stock", "low-stock", "out-of-stock"];
    private static readonly string[] Sorts = ["newest", "oldest", "name-asc", "name-desc", "price-asc", "price-desc", "stock-asc", "stock-desc"];
    private static readonly string[] StorefrontSorts = ["newest", "popular", "rating", "price-asc", "price-desc"];

    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken ct)
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
}
