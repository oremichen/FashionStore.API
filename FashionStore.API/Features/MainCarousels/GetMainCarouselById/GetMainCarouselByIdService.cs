using FashionStore.Domain.Abstractions.MainCarousels;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.MainCarousels.GetMainCarouselById;
public sealed class GetMainCarouselByIdService(IMainCarouselRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<GetMainCarouselByIdService> logger) : IGetMainCarouselByIdService
{
    private const int CarouselImageWidth = 1920;
    private const int CarouselImageHeight = 750;
    public async Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving main carousel {CarouselId}.", id);
        var response = new ResponseResult<MainCarouselResponse>();
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogError("Main carousel retrieval validation failed because carousel id is empty.");
            return response.Fail("Carousel id is required.", ResponseCodes.INVALID_ACTION);
        }

        var carousel = await repository.GetByIdAsync(id.Trim(), false, cancellationToken);
        return carousel is null ? response.Fail("Carousel was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : response.Success(Map(carousel), "Carousel retrieved successfully.");
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
