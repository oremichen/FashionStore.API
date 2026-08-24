namespace FashionStore.API.Features.PromotionBanners.GetPromotionBanners;

public interface IGetPromotionBannersService
{
    Task<ResponseResult<IReadOnlyList<PromotionBannerResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
