namespace FashionStore.API.Features.PromotionBanners.DeletePromotionBanner;

public interface IDeletePromotionBannerService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
