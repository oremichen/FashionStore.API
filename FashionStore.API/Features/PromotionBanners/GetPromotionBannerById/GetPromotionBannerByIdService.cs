namespace FashionStore.API.Features.PromotionBanners.GetPromotionBannerById;

public sealed class GetPromotionBannerByIdService(PromotionBannerOperations operations) : IGetPromotionBannerByIdService
{
    public Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.GetByIdAsync(id, cancellationToken);
}
