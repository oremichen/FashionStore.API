using FashionStore.Domain.Abstractions.MainCarousels;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.MainCarousels.UpdateMainCarousel;
public sealed class UpdateMainCarouselService(IMainCarouselRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<UpdateMainCarouselService> logger) : IUpdateMainCarouselService
{
    private const int CarouselImageWidth = 1920;
    private const int CarouselImageHeight = 750;
    public async Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(string id, UpdateMainCarouselRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating main carousel {CarouselId}.", id);
        var response = new ResponseResult<MainCarouselResponse>();
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogError("Main carousel update validation failed because carousel id is empty.");
            return response.Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
        }

        var carousel = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (carousel is null)
            return response.Fail("Carousel was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        try
        {
            carousel.SetDetails(request.Title, request.Subtitle, request.ButtonText, request.LinkUrl, request.SortOrder, request.IsActive);
            if (request.ImageData is { Length: > 0 })
            {
                var processedImage = await imageProcessor.CropAndResizeAsync(request.ImageData, request.ImageContentType ?? string.Empty, request.ImageFileName ?? string.Empty, CarouselImageWidth, CarouselImageHeight, allowUpscale: true, cancellationToken);
                var oldUrl = carousel.ImageUrl;
                var upload = await cloudinary.UploadWithMetadataAsync(processedImage.Data, processedImage.FileName, cancellationToken);
                carousel.SetImageUrl(upload.Url, upload.ContentType, upload.FileName, upload.FileSize, upload.Width, upload.Height);
                await cloudinary.DeleteAsync(oldUrl, cancellationToken);
            }

            await repository.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated main carousel {CarouselId}.", carousel.Id);
            return response.Success(Map(carousel), "Carousel updated successfully.");
        }
        catch (ArgumentException exception)
        {
            logger.LogError(exception, "Main carousel update validation failed for {CarouselId}.", id);
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private static MainCarouselResponse Map(MainCarousel carousel)
    {
        var hasImage = !string.IsNullOrWhiteSpace(carousel.ImageUrl);
        var imageUrl = carousel.ImageUrl;
        return new MainCarouselResponse
        {
            Id = carousel.Id,
            Title = carousel.Title,
            Subtitle = carousel.Subtitle,
            ButtonText = carousel.ButtonText,
            LinkUrl = carousel.LinkUrl,
            SortOrder = carousel.SortOrder,
            IsActive = carousel.IsActive,
            HasImage = hasImage,
            ImageUrl = imageUrl,
            Image = imageUrl ?? string.Empty,
            ImageWidth = hasImage ? carousel.ImageWidth : null,
            ImageHeight = hasImage ? carousel.ImageHeight : null,
            CreatedAt = carousel.CreatedAt,
            UpdatedAt = carousel.UpdatedAt
        };
    }
}
