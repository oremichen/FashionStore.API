using FashionStore.API.Features.MainCarousels.CreateMainCarousel;
using FashionStore.API.Features.MainCarousels.DeleteMainCarousel;
using FashionStore.API.Features.MainCarousels.GetMainCarouselById;
using FashionStore.API.Features.MainCarousels.GetMainCarousels;
using FashionStore.API.Features.MainCarousels.UpdateMainCarousel;

namespace FashionStore.API.Features.MainCarousels;

[Route("api/main-carousels")]
[ApiController]
public sealed class MainCarouselsController(IGetMainCarouselsService getMainCarouselsService, IGetMainCarouselByIdService getMainCarouselByIdService, ICreateMainCarouselService createMainCarouselService, IUpdateMainCarouselService updateMainCarouselService, IDeleteMainCarouselService deleteMainCarouselService) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<MainCarouselResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await getMainCarouselsService.ExecuteAsync(cancellationToken);
        return ProcessResponse(response);
    }

    [AllowAnonymous, HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<MainCarouselResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<MainCarouselResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var response = await getMainCarouselByIdService.ExecuteAsync(id, cancellationToken);
        return ProcessResponse(response);
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    [EnableRateLimiting(RateLimitPolicies.AdminUpload)]
    [Consumes("multipart/form-data"), RequestSizeLimit(5 * 1024 * 1024)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<MainCarouselResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<MainCarouselResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromForm] CreateMainCarouselForm form, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(form.Image, cancellationToken);
        var request = new CreateMainCarouselRequest
        {
            Title = form.Title, Subtitle = form.Subtitle, ButtonText = form.ButtonText, LinkUrl = form.LinkUrl,
            SortOrder = form.SortOrder, IsActive = form.IsActive, ImageData = data!,
            ImageContentType = form.Image.ContentType, ImageFileName = form.Image.FileName
        };
        return ProcessResponse(await createMainCarouselService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPut("{id}")]
    [EnableRateLimiting(RateLimitPolicies.AdminUpload)]
    [Consumes("multipart/form-data"), RequestSizeLimit(5 * 1024 * 1024)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<MainCarouselResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<MainCarouselResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<MainCarouselResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(string id, [FromForm] UpdateMainCarouselForm form, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(form.Image, cancellationToken);
        var request = new UpdateMainCarouselRequest
        {
            Title = form.Title, Subtitle = form.Subtitle, ButtonText = form.ButtonText, LinkUrl = form.LinkUrl,
            SortOrder = form.SortOrder, IsActive = form.IsActive, ImageData = data,
            ImageContentType = form.Image?.ContentType, ImageFileName = form.Image?.FileName
        };
        return ProcessResponse(await updateMainCarouselService.ExecuteAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var response = await deleteMainCarouselService.ExecuteAsync(id, cancellationToken);
        return ProcessResponse(response);
    }

    private static async Task<byte[]?> ReadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return null;
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }
}
