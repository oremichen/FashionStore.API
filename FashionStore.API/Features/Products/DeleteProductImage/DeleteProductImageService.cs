using FashionStore.Domain.Abstractions.Images;
using FashionStore.Domain.Abstractions.Products;

namespace FashionStore.API.Features.Products.DeleteProductImage;

public sealed class DeleteProductImageService(
    IProductRepository repository,
    ICloudinaryImageService cloudinary,
    ILogger<DeleteProductImageService> logger) : IDeleteProductImageService
{
    public async Task<ResponseResult> ExecuteAsync(
        string productId,
        string imageId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting image {ImageId} from product {ProductId}.", imageId, productId);

        var image = await repository.GetImageAsync(productId, imageId, cancellationToken);
        if (image is null)
        {
            logger.LogWarning("Image {ImageId} was not found for product {ProductId}.", imageId, productId);
            return new ResponseResult().Fail(
                "Product image was not found.",
                ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        if (await repository.GetImageCountAsync(productId, cancellationToken) <= 1)
        {
            logger.LogWarning(
                "Image {ImageId} cannot be deleted because it is the only image for product {ProductId}.",
                imageId,
                productId);

            return new ResponseResult().Fail(
                "A product must have at least one image. Delete the product instead if you want to remove its only image.",
                ResponseCodes.INVALID_ACTION);
        }

        await repository.DeleteImageAsync(image, cancellationToken);
        await cloudinary.DeleteAsync(image.SmallUrl, cancellationToken);
        await cloudinary.DeleteAsync(image.MediumUrl, cancellationToken);
        await cloudinary.DeleteAsync(image.BigUrl, cancellationToken);

        logger.LogInformation("Deleted image {ImageId} from product {ProductId}.", imageId, productId);
        return new ResponseResult().Success("Product image deleted successfully.");
    }
}
