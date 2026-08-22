namespace FashionStore.Application.Abstractions.PromotionVideos;

public interface IPromotionVideoService
{
    Task<ResponseResult<PagedResponse<PromotionVideoResponse>>> GetAllAsync(PromotionVideoQuery query, CancellationToken cancellationToken);
    Task<ResponseResult<PromotionVideoResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<ResponseResult<PromotionVideoResponse>> GetActiveAsync(CancellationToken cancellationToken);
    Task<ResponseResult<PromotionVideoResponse>> CreateAsync(CreatePromotionVideoRequest request, CancellationToken cancellationToken);
    Task<ResponseResult<PromotionVideoResponse>> UpdateAsync(string id, UpdatePromotionVideoRequest request, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken);
}
