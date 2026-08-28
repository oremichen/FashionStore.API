using FashionStore.Domain.Abstractions.PromotionVideos;
using FashionStore.Domain.Abstractions.Videos;

namespace FashionStore.API.Features.PromotionVideos.UpdatePromotionVideo;
public sealed class UpdatePromotionVideoService(IPromotionVideoRepository repository, ICloudinaryVideoService cloudinary) : IUpdatePromotionVideoService
{
    public async Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(string id, UpdatePromotionVideoRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionVideoResponse>();
        if (string.IsNullOrWhiteSpace(id))
            return response.Fail("Promotion video id is required.", ResponseCodes.INVALID_ACTION);
        var video = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (video is null)
            return response.Fail("Promotion video was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await repository.SlugExistsAsync(slug, video.Id, cancellationToken))
            return response.Fail("A promotion video with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        try
        {
            var isActive = request.IsActive ?? false;
            if (isActive)
                await DeactivateAllAsync(video.Id, cancellationToken);
            video.UpdateDetails(request.Title, slug, isActive, request.ExpiresAt);
            await repository.SaveChangesAsync(cancellationToken);
            return response.Success(Map(video), "Promotion video updated successfully.");
        }
        catch (ArgumentException exception)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private async Task DeactivateAllAsync(string? excludedId, CancellationToken cancellationToken)
    {
        foreach (var item in await repository.GetAllAsync(true, cancellationToken))
            if (item.IsActive && item.Id != excludedId)
                item.Deactivate();
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
