using FashionStore.Domain.Abstractions.PromotionVideos;
using FashionStore.Domain.Abstractions.Videos;

namespace FashionStore.API.Features.PromotionVideos;

public sealed class PromotionVideoOperations(IPromotionVideoRepository repository, ICloudinaryVideoService cloudinary)
{
    public async Task<ResponseResult<PagedResponse<PromotionVideoResponse>>> GetAllAsync(PromotionVideoQuery query, CancellationToken cancellationToken)
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
        return new ResponseResult<PagedResponse<PromotionVideoResponse>>().Success(
            response, "Promotion videos retrieved successfully.");
    }

    public async Task<ResponseResult<PromotionVideoResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionVideoResponse>();
        if (string.IsNullOrWhiteSpace(slug)) return response.Fail("Promotion video slug is required.", ResponseCodes.INVALID_ACTION);
        var video = await repository.GetBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken);
        return video is null ? response.Fail("Promotion video was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : response.Success(Map(video));
    }

    public async Task<ResponseResult<PromotionVideoResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionVideoResponse>();
        var video = await repository.GetActiveAsync(cancellationToken);
        return video is null ? response.Fail("There is no active promotion video.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : response.Success(Map(video));
    }

    public async Task<ResponseResult<PromotionVideoResponse>> CreateAsync(CreatePromotionVideoRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionVideoResponse>();
        var validation = ValidateVideo(request.VideoData, request.VideoContentType, request.VideoFileName);
        if (validation is not null) return response.Fail(validation, ResponseCodes.INVALID_ACTION);
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await repository.SlugExistsAsync(slug, null, cancellationToken))
            return response.Fail("A promotion video with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        try
        {
            if (request.IsActive) await DeactivateAllAsync(null, cancellationToken);
            var video = PromotionVideo.Create(request.Title, slug, request.IsActive, request.ExpiresAt);
            var upload = await cloudinary.UploadAsync(request.VideoData, request.VideoFileName, request.VideoContentType, cancellationToken);
            video.SetVideo(upload.Url, upload.ContentType, upload.FileName, upload.FileSize);
            await repository.AddAsync(video, cancellationToken);
            return response.Success(Map(video), "Promotion video created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception) { return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION); }
    }

    public async Task<ResponseResult<PromotionVideoResponse>> UpdateAsync(string id, UpdatePromotionVideoRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionVideoResponse>();
        if (string.IsNullOrWhiteSpace(id)) return response.Fail("Promotion video id is required.", ResponseCodes.INVALID_ACTION);
        var video = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (video is null) return response.Fail("Promotion video was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await repository.SlugExistsAsync(slug, video.Id, cancellationToken))
            return response.Fail("A promotion video with this slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        try
        {
            var isActive = request.IsActive ?? false;
            if (isActive) await DeactivateAllAsync(video.Id, cancellationToken);
            video.UpdateDetails(request.Title, slug, isActive, request.ExpiresAt);
            await repository.SaveChangesAsync(cancellationToken);
            return response.Success(Map(video), "Promotion video updated successfully.");
        }
        catch (ArgumentException exception) { return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION); }
    }

    public async Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return new ResponseResult().Fail("Promotion video id is required.", ResponseCodes.INVALID_ACTION);
        var video = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (video is null) return new ResponseResult().Fail("Promotion video was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        await repository.DeleteAsync(video, cancellationToken);
        await cloudinary.DeleteAsync(video.VideoUrl, cancellationToken);
        return new ResponseResult().Success("Promotion video deleted successfully.");
    }

    private async Task DeactivateAllAsync(string? excludedId, CancellationToken cancellationToken)
    {
        foreach (var item in await repository.GetAllAsync(true, cancellationToken))
            if (item.IsActive && item.Id != excludedId) item.Deactivate();
    }

    private static string? ValidateVideo(byte[]? data, string? contentType, string? fileName)
    {
        if (data is not { Length: > 0 }) return "Promotion video data is required.";
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            return "Only video files can be uploaded.";
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".webm", ".mov", ".m4v", ".avi", ".mpeg", ".mpg" };
        return string.IsNullOrWhiteSpace(fileName) || !allowed.Contains(Path.GetExtension(fileName))
            ? "The video file type is not supported." : null;
    }

    private static PromotionVideoResponse Map(PromotionVideo video)
    {
        return new PromotionVideoResponse
        {
            Id = video.Id, Title = video.Title, Slug = video.Slug, VideoUrl = video.VideoUrl,
            IsActive = video.IsActive, HasExpired = video.HasExpired, ExpiresAt = video.ExpiresAt,
            CreatedAt = video.CreatedAt, UpdatedAt = video.UpdatedAt
        };
    }
}
