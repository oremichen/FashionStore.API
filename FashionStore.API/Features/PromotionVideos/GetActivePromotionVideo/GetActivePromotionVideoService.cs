namespace FashionStore.API.Features.PromotionVideos.GetActivePromotionVideo;

public sealed class GetActivePromotionVideoService(PromotionVideoOperations operations) : IGetActivePromotionVideoService
{
    public Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(CancellationToken cancellationToken) => operations.GetActiveAsync(cancellationToken);
}
