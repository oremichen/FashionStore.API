using FashionStore.Domain.Abstractions.PromotionVideos;
using FashionStore.Domain.Abstractions.Videos;

namespace FashionStore.API.Features.PromotionVideos.CreatePromotionVideo;
public sealed class CreatePromotionVideoService(IPromotionVideoRepository repository, ICloudinaryVideoService cloudinary) : ICreatePromotionVideoService
{
    public async Task<ResponseResult<PromotionVideoResponse>> ExecuteAsync(CreatePromotionVideoRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionVideoResponse>();
        var validation = ValidateVideo(request.VideoData, request.VideoContentType, request.VideoFileName);
        if (validation is not null)
            return response.Fail(validation, ResponseCodes.INVALID_ACTION);
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await repository.SlugExistsAsync(slug, null, cancellationToken))
            return response.Fail("A promotion video with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        try
        {
            if (request.IsActive)
                await DeactivateAllAsync(null, cancellationToken);
            var video = PromotionVideo.Create(request.Title, slug, request.IsActive, request.ExpiresAt);
            var upload = await cloudinary.UploadAsync(request.VideoData, request.VideoFileName, request.VideoContentType, cancellationToken);
            video.SetVideo(upload.Url, upload.ContentType, upload.FileName, upload.FileSize);
            await repository.AddAsync(video, cancellationToken);
            return response.Success(Map(video), "Promotion video created successfully.").SetStatusCode(ResponseCodes.CREATED);
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

    private static string? ValidateVideo(byte[]? data, string? contentType, string? fileName)
    {
        if (data is not { Length: > 0 })
            return "Promotion video data is required.";
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            return "Only video files can be uploaded.";
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4",
            ".webm",
            ".mov",
            ".m4v",
            ".avi",
            ".mpeg",
            ".mpg"
        };
        return string.IsNullOrWhiteSpace(fileName) || !allowed.Contains(Path.GetExtension(fileName)) ? "The video file type is not supported." : null;
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
