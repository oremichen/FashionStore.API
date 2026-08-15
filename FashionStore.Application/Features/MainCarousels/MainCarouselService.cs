using FashionStore.Application.Abstractions.MainCarousels;
using FashionStore.Application.Abstractions.Images;

namespace FashionStore.Application.Features.MainCarousels;

public sealed class MainCarouselService(
    IMainCarouselRepository repository,
    IImageProcessor imageProcessor,
    ILogger<MainCarouselService> logger) : IMainCarouselService
{
    private const int CarouselImageWidth = 1920;
    private const int CarouselImageHeight = 750;

    public async Task<ResponseResult<IReadOnlyList<MainCarouselResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving main carousels.");
        var carousels = await repository.GetAllAsync(cancellationToken);
        var result = carousels.Select(Map).ToList();

        return new ResponseResult<IReadOnlyList<MainCarouselResponse>>()
            .Success(result, "Carousels retrieved successfully.");
    }

    public async Task<ResponseResult<MainCarouselResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving main carousel {CarouselId}.", id);
        var response = new ResponseResult<MainCarouselResponse>();
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogError("Main carousel retrieval validation failed because carousel id is empty.");
            return response.Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
        }
        var carousel = await repository.GetByIdAsync(id.Trim(), false, cancellationToken);
        return carousel is null
            ? response.Fail("Carousel was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD)
            : response.Success(Map(carousel), "Carousel retrieved successfully.");
    }

    public async Task<ResponseResult<MainCarouselResponse>> CreateAsync(CreateMainCarouselRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating main carousel {Title}.", request.Title);
        var response = new ResponseResult<MainCarouselResponse>();
        try
        {
            var processedImage = await imageProcessor.CropAndResizeAsync(
                request.ImageData,
                request.ImageContentType,
                request.ImageFileName,
                CarouselImageWidth,
                CarouselImageHeight,
                allowUpscale: true,
                cancellationToken);

            var carousel = MainCarousel.Create(request.Title, request.Subtitle, request.ButtonText, request.LinkUrl, request.SortOrder, request.IsActive);
            carousel.SetImage(
                processedImage.Data,
                processedImage.ContentType,
                processedImage.FileName,
                processedImage.Width,
                processedImage.Height);
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

    public async Task<ResponseResult<MainCarouselResponse>> UpdateAsync(string id, UpdateMainCarouselRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating main carousel {CarouselId}.", id);
        var response = new ResponseResult<MainCarouselResponse>();
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogError("Main carousel update validation failed because carousel id is empty.");
            return response.Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
        }
        var carousel = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (carousel is null) return response.Fail("Carousel was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        try
        {
            carousel.SetDetails(request.Title, request.Subtitle, request.ButtonText, request.LinkUrl, request.SortOrder, request.IsActive);
            if (request.ImageData is { Length: > 0 })
            {
                var processedImage = await imageProcessor.CropAndResizeAsync(
                    request.ImageData,
                    request.ImageContentType ?? string.Empty,
                    request.ImageFileName ?? string.Empty,
                    CarouselImageWidth,
                    CarouselImageHeight,
                    allowUpscale: true,
                    cancellationToken);

                carousel.SetImage(
                    processedImage.Data,
                    processedImage.ContentType,
                    processedImage.FileName,
                    processedImage.Width,
                    processedImage.Height);
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

    public async Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting main carousel {CarouselId}.", id);
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogError("Main carousel deletion validation failed because carousel id is empty.");
            return new ResponseResult().Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
        }
        var carousel = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (carousel is null) return new ResponseResult().Fail("Carousel was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        await repository.DeleteAsync(carousel, cancellationToken);
        logger.LogInformation("Deleted main carousel {CarouselId}.", carousel.Id);
        return new ResponseResult().Success("Carousel deleted successfully.");
    }

    public async Task<MainCarouselImageResponse?> GetImageAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving image for main carousel {CarouselId}.", id);
        if (string.IsNullOrWhiteSpace(id)) return null;
        var carousel = await repository.GetByIdAsync(id.Trim(), false, cancellationToken);
        return carousel is null ? null : new(carousel.ImageData, carousel.ImageContentType, carousel.ImageFileName);
    }

    private static MainCarouselResponse Map(MainCarousel carousel)
    {
        var hasImage = carousel.ImageData is { Length: > 0 };
        var imageUrl = hasImage ? $"/api/main-carousels/{carousel.Id}/image" : null;

        return new MainCarouselResponse
        {
            Id = carousel.Id, Title = carousel.Title, Subtitle = carousel.Subtitle,
            ButtonText = carousel.ButtonText, LinkUrl = carousel.LinkUrl, SortOrder = carousel.SortOrder,
            IsActive = carousel.IsActive, HasImage = hasImage, ImageUrl = imageUrl, Image = imageUrl ?? string.Empty,
            ImageWidth = hasImage ? carousel.ImageWidth : null, ImageHeight = hasImage ? carousel.ImageHeight : null,
            CreatedAt = carousel.CreatedAt, UpdatedAt = carousel.UpdatedAt
        };
    }
}
