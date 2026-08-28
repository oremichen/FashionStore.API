namespace FashionStore.API.Features.PromotionVideos.UpdatePromotionVideo;

public interface IUpdatePromotionVideoService
{
    Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(string id, UpdatePromotionVideoRequest request, CancellationToken cancellationToken);
}
