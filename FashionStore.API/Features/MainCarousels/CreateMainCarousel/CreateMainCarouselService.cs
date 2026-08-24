using FashionStore.Domain.Abstractions.MainCarousels;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.MainCarousels.CreateMainCarousel;
public sealed class CreateMainCarouselService(IMainCarouselRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<CreateMainCarouselService> logger) : ICreateMainCarouselService
{
    private const int CarouselImageWidth = 1920;
    private const int CarouselImageHeight = 750;
    public async Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(CreateMainCarouselRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating main carousel {Title}.", request.Title);
        var response = new ResponseResult<MainCarouselResponse>();
        try
        {
            var processedImage = await imageProcessor.CropAndResizeAsync(request.ImageData, request.ImageContentType, request.ImageFileName, CarouselImageWidth, CarouselImageHeight, allowUpscale: true, cancellationToken);
            var carousel = MainCarousel.Create(request.Title, request.Subtitle, request.ButtonText, request.LinkUrl, request.SortOrder, request.IsActive);
            var upload = await cloudinary.UploadWithMetadataAsync(processedImage.Data, processedImage.FileName, cancellationToken);
            carousel.SetImageUrl(upload.Url, upload.ContentType, upload.FileName, upload.FileSize, upload.Width, upload.Height);
            await repository.AddAsync(carousel, cancellationToken);
            logger.LogInformation("Created main carousel {CarouselId}.", carousel.Id);
            return response.Success(Map(carousel), "Carousel created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception)
        {
            logger.LogError(exception, "Main carousel creation validation failed.");
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
