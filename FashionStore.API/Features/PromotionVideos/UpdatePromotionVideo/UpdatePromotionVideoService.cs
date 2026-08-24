namespace FashionStore.API.Features.PromotionVideos.UpdatePromotionVideo;

public sealed class UpdatePromotionVideoService(PromotionVideoOperations operations) : IUpdatePromotionVideoService
{
    public Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(string id, UpdatePromotionVideoRequest request, CancellationToken cancellationToken) => operations.UpdateAsync(id, request, cancellationToken);
}
