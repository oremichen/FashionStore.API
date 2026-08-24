namespace FashionStore.API.Features.PromotionVideos.GetActivePromotionVideo;

public interface IGetActivePromotionVideoService
{
    Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(CancellationToken cancellationToken);
}
