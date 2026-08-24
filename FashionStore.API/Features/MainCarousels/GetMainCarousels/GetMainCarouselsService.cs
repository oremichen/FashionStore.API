using FashionStore.Domain.Abstractions.MainCarousels;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.MainCarousels.GetMainCarousels;
public sealed class GetMainCarouselsService(IMainCarouselRepository repository, IImageProcessor imageProcessor, ICloudinaryImageService cloudinary, ILogger<GetMainCarouselsService> logger) : IGetMainCarouselsService
{
    private const int CarouselImageWidth = 1920;
    private const int CarouselImageHeight = 750;
    public async Task<ResponseResult<IReadOnlyList<MainCarouselResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving main carousels.");
        var carousels = await repository.GetAllAsync(cancellationToken);
        var result = carousels.Select(Map).ToList();
        return new ResponseResult<IReadOnlyList<MainCarouselResponse>>().Success(result, "Carousels retrieved successfully.");
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
