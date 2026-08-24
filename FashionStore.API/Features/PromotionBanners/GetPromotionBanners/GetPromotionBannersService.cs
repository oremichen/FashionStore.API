using FashionStore.Domain.Abstractions.PromotionBanners;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.PromotionBanners.GetPromotionBanners;
public sealed class GetPromotionBannersService(IPromotionBannerRepository repository, ICloudinaryImageService cloudinary) : IGetPromotionBannersService
{
    public async Task<ResponseResult<IReadOnlyList<PromotionBannerResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var banners = await repository.GetAllAsync(cancellationToken);
        return new ResponseResult<IReadOnlyList<PromotionBannerResponse>>().Success(banners.Select(Map).ToList(), "Promotion banners retrieved successfully.");
    }

    private static PromotionBannerResponse Map(PromotionBanner banner)
    {
        return new PromotionBannerResponse
        {
            Id = banner.Id,
            Title = banner.Title,
            Subtitle = banner.Subtitle,
            Image = banner.ImageUrl ?? string.Empty,
            DestinationUrl = banner.DestinationUrl,
            Placement = banner.Placement,
            Slot = banner.Slot,
            IsActive = banner.IsActive
        };
    }
}
