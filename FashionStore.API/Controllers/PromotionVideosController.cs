using FashionStore.Application.Abstractions.PromotionVideos;

namespace FashionStore.API.Controllers;

[Route("api/promotion-videos")]
[ApiController]
public sealed class PromotionVideosController(IPromotionVideoService service) : BaseApiController
{
    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PromotionVideoQuery query, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.GetAllAsync(query, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.GetBySlugAsync(slug, cancellationToken));
    }

    [AllowAnonymous, HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.GetActiveAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    [Consumes("multipart/form-data"), RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreatePromotionVideoForm form, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(form.Video, cancellationToken);
        var request = new CreatePromotionVideoRequest(form.Title, form.Slug, form.IsActive ?? false, form.ExpiresAt,
            data, form.Video?.ContentType ?? string.Empty, form.Video?.FileName ?? string.Empty);
        return ProcessResponse(await service.CreateAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePromotionVideoRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.UpdateAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.DeleteAsync(id, cancellationToken));
    }

    private static async Task<byte[]> ReadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return [];
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }
}
