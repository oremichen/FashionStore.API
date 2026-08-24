namespace FashionStore.API.Features.PromotionVideos.DeletePromotionVideo;

public sealed class DeletePromotionVideoService(PromotionVideoOperations operations) : IDeletePromotionVideoService
{
    public Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.DeleteAsync(id, cancellationToken);
}
