namespace FashionStore.API.Features.PromotionBanners.UpdatePromotionBanner;

public interface IUpdatePromotionBannerService
{
    Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(string id, UpdatePromotionBannerRequest request, CancellationToken cancellationToken);
}
