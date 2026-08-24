using FashionStore.Domain.Abstractions.PromotionVideos;
using FashionStore.Domain.Abstractions.Videos;

namespace FashionStore.API.Features.PromotionVideos.DeletePromotionVideo;
public sealed class DeletePromotionVideoService(IPromotionVideoRepository repository, ICloudinaryVideoService cloudinary) : IDeletePromotionVideoService
{
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return new ResponseResult().Fail("Promotion video id is required.", ResponseCodes.INVALID_ACTION);
        var video = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (video is null)
            return new ResponseResult().Fail("Promotion video was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        await repository.DeleteAsync(video, cancellationToken);
        await cloudinary.DeleteAsync(video.VideoUrl, cancellationToken);
        return new ResponseResult().Success("Promotion video deleted successfully.");
    }
}
