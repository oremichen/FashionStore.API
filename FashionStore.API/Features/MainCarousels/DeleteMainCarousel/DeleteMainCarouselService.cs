using FashionStore.Domain.Abstractions.MainCarousels;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.MainCarousels.DeleteMainCarousel;
public sealed class DeleteMainCarouselService(IMainCarouselRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<DeleteMainCarouselService> logger) : IDeleteMainCarouselService
{
    private const int CarouselImageWidth = 1920;
    private const int CarouselImageHeight = 750;
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting main carousel {CarouselId}.", id);
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogError("Main carousel deletion validation failed because carousel id is empty.");
            return new ResponseResult().Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
        }

        var carousel = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (carousel is null)
            return new ResponseResult().Fail("Carousel was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        await repository.DeleteAsync(carousel, cancellationToken);
        await cloudinary.DeleteAsync(carousel.ImageUrl, cancellationToken);
        logger.LogInformation("Deleted main carousel {CarouselId}.", carousel.Id);
        return new ResponseResult().Success("Carousel deleted successfully.");
    }
}
