namespace FashionStore.API.Features.PromotionBanners.CreatePromotionBanner;

public sealed class CreatePromotionBannerService(PromotionBannerOperations operations) : ICreatePromotionBannerService
{
    public Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(CreatePromotionBannerRequest request, CancellationToken cancellationToken) => operations.CreateAsync(request, cancellationToken);
}
