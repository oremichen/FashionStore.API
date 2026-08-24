namespace FashionStore.API.Features.PromotionBanners.DeletePromotionBanner;

public sealed class DeletePromotionBannerService(PromotionBannerOperations operations) : IDeletePromotionBannerService
{
    public Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.DeleteAsync(id, cancellationToken);
}
