namespace FashionStore.API.Features.PromotionBanners.UpdatePromotionBanner;

public sealed class UpdatePromotionBannerService(PromotionBannerOperations operations) : IUpdatePromotionBannerService
{
    public Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(string id, UpdatePromotionBannerRequest request, CancellationToken cancellationToken) => operations.UpdateAsync(id, request, cancellationToken);
}
