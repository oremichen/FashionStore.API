using FashionStore.Domain.Abstractions.PromotionVideos;
using FashionStore.Domain.Abstractions.Videos;

namespace FashionStore.API.Features.PromotionVideos.GetPromotionVideos;
public sealed class GetPromotionVideosService(IPromotionVideoRepository repository, ICloudinaryVideoService cloudinary) : IGetPromotionVideosService
{
    public async Task<ResponseResult<PagedResponse<PromotionVideoResponse>>> ExecuteAsync(PromotionVideoQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetPagedAsync(query, cancellationToken);
        var response = new PagedResponse<PromotionVideoResponse>
        {
            Items = result.Items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)query.PageSize)
        };
        return new ResponseResult<PagedResponse<PromotionVideoResponse>>().Success(response, "Promotion videos retrieved successfully.");
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
