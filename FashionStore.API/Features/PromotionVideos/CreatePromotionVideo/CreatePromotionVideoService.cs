namespace FashionStore.API.Features.PromotionVideos.CreatePromotionVideo;

public sealed class CreatePromotionVideoService(PromotionVideoOperations operations) : ICreatePromotionVideoService
{
    public Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(CreatePromotionVideoRequest request, CancellationToken cancellationToken) => operations.CreateAsync(request, cancellationToken);
}
