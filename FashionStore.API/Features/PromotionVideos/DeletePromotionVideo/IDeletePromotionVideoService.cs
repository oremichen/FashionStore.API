namespace FashionStore.API.Features.PromotionVideos.DeletePromotionVideo;

public interface IDeletePromotionVideoService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
