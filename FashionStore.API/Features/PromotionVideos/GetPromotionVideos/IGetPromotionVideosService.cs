namespace FashionStore.API.Features.PromotionVideos.GetPromotionVideos;

public interface IGetPromotionVideosService
{
    Task<ResponseResult<PagedResponse<PromotionVideoResponse>>> ExecuteAsync(PromotionVideoQuery query, CancellationToken cancellationToken);
}
