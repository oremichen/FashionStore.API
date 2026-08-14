using FashionStore.Application.Abstractions.MainCarousels;
using FashionStore.Application.Abstractions.Images;

namespace FashionStore.Application.Features.MainCarousels;

public sealed class MainCarouselService(IMainCarouselRepository repository, IImageProcessor imageProcessor) : IMainCarouselService
{
    private const int CarouselImageWidth = 1920;
    private const int CarouselImageHeight = 750;

    public async Task<ResponseResult<IReadOnlyList<MainCarouselResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var carousels = await repository.GetAllAsync(cancellationToken);
        var result = carousels.Select(Map).ToList();

        return new ResponseResult<IReadOnlyList<MainCarouselResponse>>()
            .Success(result, "Carousels retrieved successfully.");
    }

    public async Task<ResponseResult<MainCarouselResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<MainCarouselResponse>();
        if (string.IsNullOrWhiteSpace(id)) return response.Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
        var carousel = await repository.GetByIdAsync(id.Trim(), false, cancellationToken);
        return carousel is null
            ? response.Fail("Carousel was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD)
            : response.Success(Map(carousel), "Carousel retrieved successfully.");
    }

    public async Task<ResponseResult<MainCarouselResponse>> CreateAsync(CreateMainCarouselRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<MainCarouselResponse>();
        try
        {
            var processedImage = await imageProcessor.CropAndResizeAsync(
                request.ImageData,
                request.ImageContentType,
                request.ImageFileName,
                CarouselImageWidth,
                CarouselImageHeight,
                cancellationToken);

            var carousel = MainCarousel.Create(request.Title, request.Subtitle, request.ButtonText, request.LinkUrl, request.SortOrder, request.IsActive);
            carousel.SetImage(
                processedImage.Data,
                processedImage.ContentType,
                processedImage.FileName,
                processedImage.Width,
                processedImage.Height);
            await repository.AddAsync(carousel, cancellationToken);
            return response.Success(Map(carousel), "Carousel created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception) { return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION); }
    }

    public async Task<ResponseResult<MainCarouselResponse>> UpdateAsync(string id, UpdateMainCarouselRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<MainCarouselResponse>();
        if (string.IsNullOrWhiteSpace(id)) return response.Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
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
                    cancellationToken);

                carousel.SetImage(
                    processedImage.Data,
                    processedImage.ContentType,
                    processedImage.FileName,
                    processedImage.Width,
                    processedImage.Height);
            }
            await repository.SaveChangesAsync(cancellationToken);
            return response.Success(Map(carousel), "Carousel updated successfully.");
        }
        catch (ArgumentException exception) { return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION); }
    }

    public async Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return new ResponseResult().Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
        var carousel = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (carousel is null) return new ResponseResult().Fail("Carousel was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        await repository.DeleteAsync(carousel, cancellationToken);
        return new ResponseResult().Success("Carousel deleted successfully.");
    }

    public async Task<MainCarouselImageResponse?> GetImageAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var carousel = await repository.GetByIdAsync(id.Trim(), false, cancellationToken);
        return carousel is null ? null : new(carousel.ImageData, carousel.ImageContentType, carousel.ImageFileName);
    }

    private static MainCarouselResponse Map(MainCarousel carousel)
    {
        return new MainCarouselResponse(
            carousel.Id,
            carousel.Title,
            carousel.Subtitle,
            carousel.ButtonText,
            carousel.LinkUrl,
            $"/api/main-carousels/{carousel.Id}/image");
    }
}
