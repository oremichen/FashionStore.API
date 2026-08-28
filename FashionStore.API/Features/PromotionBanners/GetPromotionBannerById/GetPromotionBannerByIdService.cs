using FashionStore.Domain.Abstractions.PromotionBanners;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.PromotionBanners.GetPromotionBannerById;
public sealed class GetPromotionBannerByIdService(IPromotionBannerRepository repository, ICloudinaryImageService cloudinary) : IGetPromotionBannerByIdService
{
    public async Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionBannerResponse>();
        if (string.IsNullOrWhiteSpace(id))
            return response.Fail("Promotion banner id is required.", ResponseCodes.INVALID_ACTION);
        var banner = await repository.GetByIdAsync(id.Trim(), false, cancellationToken);
        return banner is null ? response.Fail("Promotion banner was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : response.Success(Map(banner));
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
