namespace FashionStore.API.Features.PromotionVideos.GetPromotionVideoBySlug;

public sealed class GetPromotionVideoBySlugService(PromotionVideoOperations operations) : IGetPromotionVideoBySlugService
{
    public Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(string slug, CancellationToken cancellationToken) => operations.GetBySlugAsync(slug, cancellationToken);
}
