namespace FashionStore.API.Features.PromotionVideos.CreatePromotionVideo;

public interface ICreatePromotionVideoService
{
    Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(CreatePromotionVideoRequest request, CancellationToken cancellationToken);
}
