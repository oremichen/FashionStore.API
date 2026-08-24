namespace FashionStore.API.Features.PromotionVideos.GetPromotionVideoBySlug;

public interface IGetPromotionVideoBySlugService
{
    Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(string slug, CancellationToken cancellationToken);
}
