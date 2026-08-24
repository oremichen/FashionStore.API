namespace FashionStore.API.Features.PromotionBanners.GetPromotionBannerById;

public interface IGetPromotionBannerByIdService
{
    Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(string id, CancellationToken cancellationToken);
}
