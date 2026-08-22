namespace FashionStore.Application.Abstractions.PromotionBanners;

public interface IPromotionBannerService
{
    Task<ResponseResult<IReadOnlyList<PromotionBannerResponse>>> GetAllAsync(CancellationToken cancellationToken);
    Task<ResponseResult<PromotionBannerResponse>> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<ResponseResult<PromotionBannerResponse>> CreateAsync(CreatePromotionBannerRequest request, CancellationToken cancellationToken);
    Task<ResponseResult<PromotionBannerResponse>> UpdateAsync(string id, UpdatePromotionBannerRequest request, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken);
}
