using FashionStore.Domain.Abstractions.PromotionBanners;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.PromotionBanners.DeletePromotionBanner;
public sealed class DeletePromotionBannerService(IPromotionBannerRepository repository, ICloudinaryImageService cloudinary) : IDeletePromotionBannerService
{
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return new ResponseResult().Fail("Promotion banner id is required.", ResponseCodes.INVALID_ACTION);
        var banner = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (banner is null)
            return new ResponseResult().Fail("Promotion banner was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        await repository.DeleteAsync(banner, cancellationToken);
        await cloudinary.DeleteAsync(banner.ImageUrl, cancellationToken);
        return new ResponseResult().Success("Promotion banner deleted successfully.");
    }
}
