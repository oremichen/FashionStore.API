namespace FashionStore.API.Features.PromotionVideos.GetPromotionVideos;

public sealed class GetPromotionVideosService(PromotionVideoOperations operations) : IGetPromotionVideosService
{
    public Task<ResponseResult<PagedResponse<PromotionVideoResponse>>> ExecuteAsync(PromotionVideoQuery query, CancellationToken cancellationToken) => operations.GetAllAsync(query, cancellationToken);
}
