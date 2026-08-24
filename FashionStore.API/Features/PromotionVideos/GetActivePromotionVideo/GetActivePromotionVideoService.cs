using FashionStore.Domain.Abstractions.PromotionVideos;
using FashionStore.Domain.Abstractions.Videos;

namespace FashionStore.API.Features.PromotionVideos.GetActivePromotionVideo;
public sealed class GetActivePromotionVideoService(IPromotionVideoRepository repository, ICloudinaryVideoService cloudinary) : IGetActivePromotionVideoService
{
    public async Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionVideoResponse>();
        var video = await repository.GetActiveAsync(cancellationToken);
        return video is null ? response.Fail("There is no active promotion video.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : response.Success(Map(video));
    }

    private static PromotionVideoResponse Map(PromotionVideo video)
    {
        return new PromotionVideoResponse
        {
            Id = video.Id,
            Title = video.Title,
            Slug = video.Slug,
            VideoUrl = video.VideoUrl,
            IsActive = video.IsActive,
            HasExpired = video.HasExpired,
            ExpiresAt = video.ExpiresAt,
            CreatedAt = video.CreatedAt,
            UpdatedAt = video.UpdatedAt
        };
    }
}
