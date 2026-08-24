namespace FashionStore.API.Features.PromotionBanners.CreatePromotionBanner;

public interface ICreatePromotionBannerService
{
    Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(CreatePromotionBannerRequest request, CancellationToken cancellationToken);
}
