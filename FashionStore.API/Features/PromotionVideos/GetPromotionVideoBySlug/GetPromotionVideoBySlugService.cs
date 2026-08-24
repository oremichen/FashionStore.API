using FashionStore.Domain.Abstractions.PromotionVideos;
using FashionStore.Domain.Abstractions.Videos;

namespace FashionStore.API.Features.PromotionVideos.GetPromotionVideoBySlug;
public sealed class GetPromotionVideoBySlugService(IPromotionVideoRepository repository, ICloudinaryVideoService cloudinary) : IGetPromotionVideoBySlugService
{
    public async Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(string slug, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionVideoResponse>();
        if (string.IsNullOrWhiteSpace(slug))
            return response.Fail("Promotion video slug is required.", ResponseCodes.INVALID_ACTION);
        var video = await repository.GetBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken);
        return video is null ? response.Fail("Promotion video was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : response.Success(Map(video));
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
