using FashionStore.API.Features.PromotionBanners.CreatePromotionBanner;
using FashionStore.API.Features.PromotionBanners.DeletePromotionBanner;
using FashionStore.API.Features.PromotionBanners.GetPromotionBannerById;
using FashionStore.API.Features.PromotionBanners.GetPromotionBanners;
using FashionStore.API.Features.PromotionBanners.UpdatePromotionBanner;

namespace FashionStore.API.Features.PromotionBanners;

[Route("api/promotion-banners")]
[ApiController]
public sealed class PromotionBannersController(IGetPromotionBannersService getAllService, IGetPromotionBannerByIdService getByIdService, ICreatePromotionBannerService createService, IUpdatePromotionBannerService updateService, IDeletePromotionBannerService deleteService) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<PromotionBannerResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return ProcessResponse(await getAllService.ExecuteAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseResult<PromotionBannerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getByIdService.ExecuteAsync(id, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    [EnableRateLimiting(RateLimitPolicies.AdminUpload)]
    [Consumes("multipart/form-data"), RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(ResponseResult<PromotionBannerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromForm] CreatePromotionBannerForm form, CancellationToken cancellationToken)
    {
        var request = new CreatePromotionBannerRequest(form.Title, form.Subtitle, form.DestinationUrl, form.Placement,
            form.Slot, form.IsActive ?? false, await ReadAsync(form.Image, cancellationToken) ?? [], form.Image.ContentType, form.Image.FileName);
        return ProcessResponse(await createService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPut("{id}")]
    [EnableRateLimiting(RateLimitPolicies.AdminUpload)]
    [Consumes("multipart/form-data"), RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(ResponseResult<PromotionBannerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(string id, [FromForm] UpdatePromotionBannerForm form, CancellationToken cancellationToken)
    {
        var request = new UpdatePromotionBannerRequest(form.Title, form.Subtitle, form.DestinationUrl, form.Placement,
            form.Slot, form.IsActive ?? false, await ReadAsync(form.Image, cancellationToken), form.Image?.ContentType, form.Image?.FileName);
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

    private static async Task<byte[]?> ReadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return null;
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }
}
