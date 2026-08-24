namespace FashionStore.API.Features.PromotionBanners.GetPromotionBanners;

public sealed class GetPromotionBannersService(PromotionBannerOperations operations) : IGetPromotionBannersService
{
    public Task<ResponseResult<IReadOnlyList<PromotionBannerResponse>>> ExecuteAsync(CancellationToken cancellationToken) => operations.GetAllAsync(cancellationToken);
}
