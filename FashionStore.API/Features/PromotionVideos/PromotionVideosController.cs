using FashionStore.API.Features.PromotionVideos.CreatePromotionVideo;
using FashionStore.API.Features.PromotionVideos.DeletePromotionVideo;
using FashionStore.API.Features.PromotionVideos.GetActivePromotionVideo;
using FashionStore.API.Features.PromotionVideos.GetPromotionVideoBySlug;
using FashionStore.API.Features.PromotionVideos.GetPromotionVideos;
using FashionStore.API.Features.PromotionVideos.UpdatePromotionVideo;

namespace FashionStore.API.Features.PromotionVideos;

[Route("api/promotion-videos")]
[ApiController]
public sealed class PromotionVideosController(IGetPromotionVideosService getAllService, IGetPromotionVideoBySlugService getBySlugService, IGetActivePromotionVideoService getActiveService, ICreatePromotionVideoService createService, IUpdatePromotionVideoService updateService, IDeletePromotionVideoService deleteService) : BaseApiController
{
    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpGet]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<PromotionVideoResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] PromotionVideoQuery query, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getAllService.ExecuteAsync(query, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ResponseResult<PromotionVideoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getBySlugService.ExecuteAsync(slug, cancellationToken));
    }

    [AllowAnonymous, HttpGet("active")]
    [ProducesResponseType(typeof(ResponseResult<PromotionVideoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        return ProcessResponse(await getActiveService.ExecuteAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    [EnableRateLimiting(RateLimitPolicies.AdminUpload)]
    [Consumes("multipart/form-data"), RequestSizeLimit(100 * 1024 * 1024)]
    [ProducesResponseType(typeof(ResponseResult<PromotionVideoResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromForm] CreatePromotionVideoForm form, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(form.Video, cancellationToken);
        var request = new CreatePromotionVideoRequest(form.Title, form.Slug, form.IsActive ?? false, form.ExpiresAt,
            data, form.Video?.ContentType ?? string.Empty, form.Video?.FileName ?? string.Empty);
        return ProcessResponse(await createService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseResult<PromotionVideoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePromotionVideoRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await updateService.ExecuteAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await deleteService.ExecuteAsync(id, cancellationToken));
    }

    private static async Task<byte[]> ReadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return [];
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }
}
